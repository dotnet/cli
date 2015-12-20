// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <set>
#include <functional>

#include "trace.h"
#include "deps_resolver.h"
#include "utils.h"

bool deps_entry_t::to_full_path(const pal::string_t& base, pal::string_t* str) const
{
    pal::string_t& candidate = *str;

    candidate.reserve(base.length() +
        library_name.length() +
        library_version.length() +
        relative_path.length() + 3);

    candidate.assign(base);
    append_path(candidate, library_name.c_str());
    append_path(candidate, library_version.c_str());
    append_path(candidate, relative_path.c_str());

    bool exists = pal::file_exists(*str);
    if (!exists)
    {
        candidate.clear();
    }
    return exists;
}

bool read_field(const pal::string_t& line, pal::char_t* buf, unsigned* ofs, pal::string_t* field)
{
    unsigned& offset = *ofs;
    pal::string_t& value_recv = *field;

    // The first character should be a '"'
    if (line[offset] != '"')
    {
        trace::error(_X("error reading TPA file"));
        return false;
    }
    offset++;

    auto buf_offset = 0;

    // Iterate through characters in the string
    for (; offset < line.length(); offset++)
    {
        // Is this a '\'?
        if (line[offset] == '\\')
        {
            // Skip this character and read the next character into the buffer
            offset++;
            buf[buf_offset] = line[offset];
        }
        // Is this a '"'?
        else if (line[offset] == '\"')
        {
            // Done! Advance to the pointer after the input
            offset++;
            break;
        }
        else
        {
            // Take the character
            buf[buf_offset] = line[offset];
        }
        buf_offset++;
    }
    buf[buf_offset] = '\0';
    value_recv.assign(buf);

    // Consume the ',' if we have one
    if (line[offset] == ',')
    {
        offset++;
    }
    return true;
}

bool deps_resolver_t::load()
{
    if (!pal::file_exists(m_deps_path))
    {
        return true;
    }

    pal::ifstream_t file(m_deps_path);
    if (!file.good())
    {
        return false;
    }

    std::string line;
    while (std::getline(file, line))
    {
        deps_entry_t entry;
        pal::string_t* fields[] = {
            &entry.library_type,
            &entry.library_name,
            &entry.library_version,
            &entry.library_hash,
            &entry.asset_type,
            &entry.asset_name,
            &entry.relative_path
        };

        auto line_palstr = pal::to_palstring(line);
        std::vector<pal::char_t> buf(line_palstr.length());

        for (unsigned i = 0, offset = 0; i < sizeof(fields) / sizeof(fields[0]); ++i)
        {
            if (!(read_field(line_palstr, buf.data(), &offset, fields[i])))
            {
                return false;
            }
        }
        m_deps_entries.push_back(entry);
    }
    return true;
}


// ----------------------------------------------------------------------
//
// parse_deps_file: Obtain the .deps file path from the app base directory
//                 and parse it
//
bool deps_resolver_t::parse_deps_file(const arguments_t& args)
{
    const auto& app_base = args.app_dir;
    auto app_name = get_filename(args.managed_application);

    m_deps_path.reserve(app_base.length() + 1 + app_name.length() + 5);
    m_deps_path.append(app_base);
    m_deps_path.push_back(DIR_SEPARATOR);
    m_deps_path.append(app_name, 0, app_name.find_last_of(_X(".")));
    m_deps_path.append(_X(".deps"));

    return load();
}

void deps_resolver_t::get_local_assemblies(const pal::string_t& dir)
{
    trace::verbose(_X("adding files from dir %s"), dir.c_str());

    const pal::string_t managed_ext[] = { _X(".ni.dll"), _X(".dll"), _X(".ni.exe"), _X(".exe") };

    std::vector<pal::string_t> files;
    pal::readdir(dir, &files);

    for (const auto& ext : managed_ext)
    {
        for (const auto& file : files)
        {
            if (file.length() <= ext.length())
            {
                continue;
            }

            auto file_name = file.substr(0, file.length() - ext.length());
            auto file_ext = file.substr(file_name.length());

            if (pal::strcasecmp(ext.c_str(), file_ext.c_str()))
            {
                continue;
            }

            // TODO: Do a case insensitive lookup.
            if (m_local_assemblies.count(file_name))
            {
                trace::verbose(_X("Skipping %s because the filename already exists in loscal assemblies"), file.c_str());
                continue;
            }

            pal::string_t file_path = dir + DIR_SEPARATOR + file;
            trace::verbose(_X("adding %s to local assembly set from %s"), file_name.c_str(), file_path.c_str());
            m_local_assemblies.emplace(file_name, file_path);
        }
    }
}

void add_tpa_asset(
    const pal::string_t& asset_name,
    const pal::string_t& asset_path,
    std::set<pal::string_t>* items,
    pal::string_t* output)
{
    if (items->count(asset_name))
    {
        return;
    }

    trace::verbose(_X("adding tpa entry: %s"), asset_path.c_str());
    output->append(asset_path);
    output->push_back(PATH_SEPARATOR);
    items->insert(asset_name);
}

void deps_resolver_t::write_tpa_list(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& clr_dir,
        pal::string_t* output)
{
    get_local_assemblies(app_dir);

    std::set<pal::string_t> items;

    pal::string_t mscorlib_path = clr_dir;
    mscorlib_path.push_back(DIR_SEPARATOR);
    mscorlib_path.append(_X("mscorlib.dll"));
    add_tpa_asset(_X("mscorlib"), mscorlib_path, &items, output);

    for (const deps_entry_t& entry : m_deps_entries)
    {
        if (entry.asset_type != _X("runtime") || items.count(entry.asset_name))
        {
            continue;
        }

        pal::string_t redirection_path, candidate;
        if (entry.library_type == _X("Package") &&
                m_svc.find_redirection(entry.library_name, entry.library_version, entry.relative_path, &redirection_path))
        {
            add_tpa_asset(entry.asset_name, redirection_path, &items, output);
        }
        else if (m_local_assemblies.count(entry.asset_name))
        {
            // TODO: Case insensitive look up?
            add_tpa_asset(entry.asset_name, m_local_assemblies.find(entry.asset_name)->second, &items, output);
        }
        else if (entry.to_full_path(package_dir, &candidate))
        {
            add_tpa_asset(entry.asset_name, candidate, &items, output);
        }
    }
    for (const auto& kv : m_local_assemblies)
    {
        add_tpa_asset(kv.first, kv.second, &items, output);
    }
}

void add_unique_path(
    const pal::string_t& type,
    const pal::string_t& path,
    std::set<pal::string_t>* existing,
    pal::string_t* output)
{
    if (existing->count(path))
    {
        return;
    }

    trace::verbose(_X("adding to %s path: %s"), type.c_str(), path.c_str());
    output->append(path);
    output->push_back(PATH_SEPARATOR);
    existing->insert(path);
}

void deps_resolver_t::write_native_paths(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& clr_dir,
        pal::string_t* output)
{
    // First take care of serviced native dlls.
    std::set<pal::string_t> items;
    for (const deps_entry_t& entry : m_deps_entries)
    {
        pal::string_t redirection_path;
        if (entry.asset_type == _X("native") && entry.library_type == _X("Package") &&
                m_svc.find_redirection(entry.library_name, entry.library_version, entry.relative_path, &redirection_path))
        {
            add_unique_path(_X("native"), get_directory(redirection_path), &items, output);
        }
    }

    // App local path
    add_unique_path(_X("native"), app_dir, &items, output);

    // Take care of the packages cached path
    for (const deps_entry_t& entry : m_deps_entries)
    {
        pal::string_t candidate;
        if (entry.asset_type == _X("native") && entry.to_full_path(package_dir, &candidate))
        {
            add_unique_path(_X("native"), get_directory(candidate), &items, output);
        }
    }

    // CLR path
    add_unique_path(_X("native"), clr_dir, &items, output);
}

void deps_resolver_t::write_culture_paths(
    const pal::string_t& app_dir,
    const pal::string_t& package_dir,
    const pal::string_t& clr_dir,
    pal::string_t* output)
{
    std::set<pal::string_t> items;
    for (const deps_entry_t& entry : m_deps_entries)
    {
        pal::string_t redirection_path;
        if (entry.asset_type == _X("culture") && entry.library_type == _X("Package") &&
                m_svc.find_redirection(entry.library_name, entry.library_version, entry.relative_path, &redirection_path))
        {
            add_unique_path(_X("culture"), get_directory(get_directory(redirection_path)), &items, output);
        }
    }

    // App local path
    add_unique_path(_X("culture"), app_dir, &items, output);
}

bool deps_resolver_t::write_probe_paths(
    const pal::string_t& app_dir,
    const pal::string_t& package_dir,
    const pal::string_t& clr_dir,
    probe_paths_t* probe_paths)
{
    write_tpa_list(app_dir, package_dir, clr_dir, &probe_paths->tpa);
    write_native_paths(app_dir, package_dir, clr_dir, &probe_paths->native);
    write_culture_paths(app_dir, package_dir, clr_dir, &probe_paths->culture);
    return true;
}
