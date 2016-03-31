// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "trace.h"
#include "utils.h"
#include "cpprest/json.h"
#include "runtime_config.h"
#include <cassert>

runtime_config_t::runtime_config_t(const pal::string_t& path)
    : m_fx_roll_fwd(true)
    , m_path(path)
    , m_portable(false)
{
    m_valid = ensure_parsed();
    trace::verbose(_X("Runtime config [%s] is valid=[%d]"), path.c_str(), m_valid);
} 

bool runtime_config_t::parse_opts(const json_value& opts)
{
    if (opts.is_null())
    {
        return true;
    }

    const auto& opts_obj = opts.as_object();
    
    auto properties = opts_obj.find(_X("configProperties"));
    if (properties != opts_obj.end())
    {
        const auto& prop_obj = properties->second.as_object();
        for (const auto& property : prop_obj)
        {
            m_properties[property.first] = property.second.is_string()
                ? property.second.as_string()
                : property.second.to_string();
        }
    }

    auto probe_paths = opts_obj.find(_X("additionalProbingPaths"));
    if (probe_paths != opts_obj.end())
    {
        if (probe_paths->second.is_string())
        {
            m_probe_paths.push_back(probe_paths->second.as_string());
        }
        else
        {
            const auto& arr = probe_paths->second.as_array();
            for (const auto& str : arr)
            {
                m_probe_paths.push_back(str.as_string());
            }
        }
    }

    auto roll_fwd = opts_obj.find(_X("rollForward"));
    if (roll_fwd != opts_obj.end())
    {
        m_fx_roll_fwd = roll_fwd->second.as_bool();
    }

    auto framework =  opts_obj.find(_X("framework"));
    if (framework == opts_obj.end())
    {
        return true;
    }

    m_portable = true;

    const auto& fx_obj = framework->second.as_object();
    m_fx_name = fx_obj.at(_X("name")).as_string();
    m_fx_ver = fx_obj.at(_X("version")).as_string();
    return true;
}

bool runtime_config_t::ensure_parsed()
{
    pal::string_t retval;
    if (!pal::file_exists(m_path))
    {
        // Not existing is not an error.
        return true;
    }

    pal::ifstream_t file(m_path);
    if (!file.good())
    {
        trace::verbose(_X("File stream not good %s"), m_path.c_str());
        return false;
    }

    try
    {
        const auto root = json_value::parse(file);
        const auto& json = root.as_object();
        const auto iter = json.find(_X("runtimeOptions"));
        if (iter != json.end())
        {
            parse_opts(iter->second);
        }
    }
    catch (const web::json::json_exception& je)
    {
        pal::string_t jes = pal::to_palstring(je.what());
        trace::error(_X("A JSON parsing exception occurred: %s"), jes.c_str());
        return false;
    }
    return true;
}

const pal::string_t& runtime_config_t::get_fx_name() const
{
    assert(m_valid);
    return m_fx_name;
}

const pal::string_t& runtime_config_t::get_fx_version() const
{
    assert(m_valid);
    return m_fx_ver;
}

bool runtime_config_t::get_fx_roll_fwd() const
{
    assert(m_valid);
    return m_fx_roll_fwd;
}

bool runtime_config_t::get_portable() const
{
    return m_portable;
}

const std::vector<pal::string_t>& runtime_config_t::get_probe_paths() const
{
    return m_probe_paths;
}

void runtime_config_t::config_kv(std::vector<std::string>* keys, std::vector<std::string>* values) const
{
    for (const auto& kv : m_properties)
    {
        keys->push_back(pal::to_stdstring(kv.first));
        values->push_back(pal::to_stdstring(kv.second));
    }
}
