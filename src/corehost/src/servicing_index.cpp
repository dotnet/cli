// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "trace.h"
#include "servicing_index.h"

servicing_index_t::servicing_index_t(const arguments_t& args)
{
    m_patch_root = args.svc_dir;
    if (!m_patch_root.empty())
    {
        m_index_file.assign(m_patch_root);
		    append_path(m_index_file, _X("dotnet_servicing_index.txt"));
    }
    m_parsed = m_index_file.empty() || !pal::file_exists(m_index_file);
}

bool servicing_index_t::find_redirection(
        const pal::string_t& package_name,
        const pal::string_t& package_version,
        const pal::string_t& package_relative,
        pal::string_t* redirection)
{
    ensure_redirections();

    if (m_redirections.empty())
    {
        return false;
    }

    pal::stringstream_t stream;
    stream << package_name << _X("|") << package_version << _X("|") << package_relative;

    auto iter = m_redirections.find(stream.str());
    if (iter != m_redirections.end())
    {
        pal::string_t full_path = m_patch_root;
        append_path(full_path, iter->second.c_str());
        if (pal::file_exists(full_path))
        {
            *redirection = full_path;
            return true;
        }
    }

    return false;
}

void servicing_index_t::ensure_redirections()
{
    if (m_parsed)
    {
        return;
    }

    pal::ifstream_t fstream(m_index_file);
    if (!fstream.good())
    {
        return;
    }

    pal::stringstream_t sstream;
    std::string line;
    while (std::getline(fstream, line))
    {
        pal::string_t str = pal::to_palstring(line);

        // Can interpret line as "package"?
        pal::string_t prefix = _X("package|");
        if (str.find(prefix) != 0)
        {
            continue;
        }

        pal::string_t name, version, relative;
        pal::string_t* tokens[] = { &name, &version, &relative };
        pal::string_t delim[] = { pal::string_t(_X("|")), pal::string_t(_X("|")), pal::string_t(_X("=")) };

        bool bad_line = false;

        size_t from = prefix.length();
        for (size_t cur = 0; cur < (sizeof(delim) / sizeof(delim[0])); ++cur)
        {
            size_t pos = str.find(delim[cur], from);
            if (pos == pal::string_t::npos)
            {
                bad_line = true;
                break;
            }

            tokens[cur]->assign(str.substr(from, pos - from));
            from = pos + 1;
        }

        if (bad_line)
        {
            trace::error(_X("Bad line in servicing index. Skipping..."));
            continue;
        }

        // Save redirection for this package.
        sstream.str(_X(""));
        sstream << name << _X("|") << version << _X("|") << relative;

        // Store just the filename.
        m_redirections.emplace(sstream.str(), str.substr(from));
    }

    m_parsed = true;
}
