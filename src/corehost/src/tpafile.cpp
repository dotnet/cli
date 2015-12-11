// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <set>
#include <functional>

#include "trace.h"
#include "tpafile.h"
#include "utils.h"

bool tpaentry_t::to_full_path(const pal::string_t& base, pal::string_t* str) const
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
    
    return pal::file_exists(*str);
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

bool tpafile::load(const pal::string_t& path)
{
    if (!pal::file_exists(path))
    {
        return true;
    }

    pal::ifstream_t file(path);
    if (!file.good())
    {
        return false;
    }

    std::string line;
    while (std::getline(file, line))
    {
        tpaentry_t entry;
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
        m_tpa_entries.push_back(entry);
    }
    return true;
}

void tpafile::get_local_assemblies(const pal::string_t& dir)
{
    trace::verbose(_X("adding files from %s to TPA"), dir.c_str());

    const pal::string_t tpa_extensions[] = { _X(".ni.dll"), _X(".dll"), _X(".ni.exe"), _X(".exe") };

    std::vector<pal::string_t> files;
    pal::readdir(dir, &files);

    for (const auto& ext : tpa_extensions)
    {
        for (const auto& file : files)
        {
            if (file.length() <= ext.length())
            {
                continue;
            }

            auto file_name = file.substr(0, file.length() - ext.length());
            if (file_name.empty() || m_local_assemblies.count(file_name))
            {
                continue;
            }

            auto file_ext = file.substr(file_name.length());
            if (pal::strcasecmp(ext.c_str(), file_ext.c_str()))
            {
                continue;
            }

            pal::string_t file_path = dir + DIR_SEPARATOR + file;            
            trace::verbose(_X("adding %s to local list from %s"), file_name.c_str(), file_path.c_str());
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

void tpafile::write_tpa_list(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& clr_dir,
        pal::string_t& output)
{
    get_local_assemblies(app_dir);

    std::set<pal::string_t> items;
    for (const tpaentry_t& entry : m_tpa_entries)
    {
        if (entry.asset_type != _X("runtime") || items.count(entry.asset_name))
        {
            continue;
        }

        pal::string_t redirection_path, candidate;
        if (entry.library_type == _X("Package") &&
                m_svc->find_redirection(entry.library_name, entry.library_version, entry.relative_path, &redirection_path))
        {
            add_tpa_asset(entry.asset_name, redirection_path, &items, &output);
        }
        else if (m_local_assemblies.count(entry.asset_name))
        {
            // TODO: Case insensitive look up?
            add_tpa_asset(entry.asset_name, m_local_assemblies.find(entry.asset_name)->second, &items, &output);
        }
        else if (entry.to_full_path(package_dir, &candidate))
        {
            add_tpa_asset(entry.asset_name, candidate, &items, &output);
        }
    }
    for (const auto& kv : m_local_assemblies)
    {
        add_tpa_asset(kv.first, kv.second, &items, &output);
    }
}

void add_native_path(
    const pal::string_t& path,
    std::set<pal::string_t>* items,
    pal::string_t* output)
{
    if (items->count(path))
    {
        return;
    }

    trace::verbose(_X("adding native search path: %s"), path.c_str());
    output->append(path);
    output->push_back(PATH_SEPARATOR);
    items->insert(path);
}

void tpafile::write_native_paths(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& clr_dir,
        pal::string_t& output)
{
    // First take care of serviced native dlls.
    std::set<pal::string_t> items;
    for (const tpaentry_t& entry : m_tpa_entries)
    {
        pal::string_t redirection_path;
        if (entry.asset_type == _X("native") && entry.library_type == _X("Package") &&
                m_svc->find_redirection(entry.library_name, entry.library_version, entry.relative_path, &redirection_path))
        {
            add_native_path(get_directory(redirection_path), &items, &output);
        }
    }

    // App local path
    add_native_path(app_dir, &items, &output);

    // Take care of the packages cached path
    for (const tpaentry_t& entry : m_tpa_entries)
    {
        pal::string_t candidate;
        if (entry.asset_type == _X("native") && entry.to_full_path(package_dir, &candidate))
        {
            add_native_path(get_directory(candidate), &items, &output);
        }
    }

    // CLR path
    add_native_path(clr_dir, &items, &output);
}

