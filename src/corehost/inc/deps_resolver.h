// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef DEPS_RESOLVER_H
#define DEPS_RESOLVER_H

#include <vector>

#include "pal.h"
#include "trace.h"

#include "servicing_index.h"

struct deps_entry_t
{
    pal::string_t library_type;
    pal::string_t library_name;
    pal::string_t library_version;
    pal::string_t library_hash;
    pal::string_t asset_type;
    pal::string_t asset_name;
    pal::string_t relative_path;

    bool to_full_path(const pal::string_t& root, pal::string_t* str) const;
};

struct probe_paths_t
{
    pal::string_t tpa;
    pal::string_t native;
    pal::string_t culture;
};

class deps_resolver_t
{
public:
    deps_resolver_t(const arguments_t& args)
        : m_svc(args.svc_dir)
    {
        m_valid = parse_deps_file(args);
    }

    bool valid() { return m_valid; }

    bool write_probe_paths(
      const pal::string_t& app_dir,
      const pal::string_t& package_dir,
      const pal::string_t& clr_dir,
      probe_paths_t* probe_paths);

private:

    bool load();

    bool parse_deps_file(const arguments_t& args);

    void write_tpa_list(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& clr_dir,
        pal::string_t* output);

    void write_native_paths(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& clr_dir,
        pal::string_t* output);

    void write_culture_paths(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& clr_dir,
        pal::string_t* output);

    void get_local_assemblies(const pal::string_t& dir);

    servicing_index_t m_svc;
    std::unordered_map<pal::string_t, pal::string_t> m_local_assemblies;
    std::vector<deps_entry_t> m_deps_entries;
    std::vector<pal::string_t> m_package_search_paths;
    pal::string_t m_deps_path;
    bool m_valid;
};

#endif // DEPS_RESOLVER_H
