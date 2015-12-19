// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "args.h"
#include "trace.h"
#include "tpafile.h"
#include "utils.h"
#include "coreclr.h"

// ----------------------------------------------------------------------
//
// get_tpafile_path: Obtain the TPA file path from the app base directory
//
void get_tpafile_path(const arguments_t& args, pal::string_t* tpapath)
{
    const auto& app_base = args.app_dir;
    auto app_name = get_filename(args.managed_application);

    tpapath->clear();
    tpapath->reserve(app_base.length() + 1 + app_name.length() + 5);
    tpapath->append(app_base);
    tpapath->push_back(DIR_SEPARATOR);
    tpapath->append(app_name, 0, app_name.find_last_of(_X(".")));
    tpapath->append(_X(".deps"));
}

// ----------------------------------------------------------------------
// resolve_clr_path: Resolve CLR Path in priority order
//
// Description:
//   Check if CoreCLR library exists in runtime servicing dir or app
//   local or DOTNET_HOME directory in that order of priority. If these
//   fail to locate CoreCLR, then check platform-specific search.
//
// Returns:
//   "true" if path to the CoreCLR dir can be resolved in "clr_path"
//    parameter. Else, returns "false" with "clr_path" unmodified.
//
bool resolve_clr_path(const arguments_t& args, pal::string_t* clr_path)
{
    const pal::string_t* dirs[] = {
        &args.svc_dir, // DOTNET_RUNTIME_SERVICING
        &args.app_dir, // APP LOCAL
        &args.home_dir // DOTNET_HOME
    };
    for (int i = 0; i < sizeof(dirs) / sizeof(dirs[0]); ++i)
    {
        if (dirs[i]->empty())
        {
            continue;
        }
        pal::string_t cur = *dirs[i];
        if (dirs[i] != &args.app_dir)
        {
            append_path(cur, _X("runtime"));
            append_path(cur, _X("coreclr"));
        }
        if (coreclr_exists_in_dir(cur))
        {
            clr_path->assign(cur);
            return true;
        }
    }

    // Use platform-specific search algorithm
    pal::string_t home_dir = args.home_dir;
    if (pal::find_coreclr(&home_dir))
    {
        clr_path->assign(home_dir);
        return true;
    }
    return false;
}


int run(const arguments_t& args, const pal::string_t& clr_path)
{
    // Check for and load deps file
    pal::string_t tpafile_path;
    get_tpafile_path(args, &tpafile_path);

    tpafile_t tpa(args);
    if (!tpa.load(tpafile_path))
    {
        trace::error(_X("invalid .deps file"));
        return 1;
    }
    // Add packages directory
    pal::string_t packages_dir;
    pal::get_default_packages_directory(packages_dir);
    trace::info(_X("Package directory %s"), packages_dir.empty() ? _X("not specified") : packages_dir.c_str());

    probe_paths_t probe_paths;
    if (!tpa.write_probe_paths(args.app_dir, packages_dir, clr_path, &probe_paths))
    {
        return 1;
    }

    // Build CoreCLR properties
    const char* property_keys[] = {
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "APP_PATHS",
        "APP_NI_PATHS",
        "NATIVE_DLL_SEARCH_DIRECTORIES",
        "PLATFORM_RESOURCE_ROOTS",
        "AppDomainCompatSwitch",
        // TODO: pipe this from corehost.json
        "SERVER_GC"
    };

    auto tpa_paths_cstr = pal::to_stdstring(probe_paths.tpa);
    auto app_base_cstr = pal::to_stdstring(args.app_dir);
    auto native_dirs_cstr = pal::to_stdstring(probe_paths.native);
    auto culture_dirs_cstr = pal::to_stdstring(probe_paths.culture);

    const char* property_values[] = {
        // TRUSTED_PLATFORM_ASSEMBLIES
        tpa_paths_cstr.c_str(),
        // APP_PATHS
        app_base_cstr.c_str(),
        // APP_NI_PATHS
        app_base_cstr.c_str(),
        // NATIVE_DLL_SEARCH_DIRECTORIES
        native_dirs_cstr.c_str(),
        // PLATFORM_RESOURCE_ROOTS
        culture_dirs_cstr.c_str(),
        // AppDomainCompatSwitch
        "UseLatestBehaviorWhenTFMNotSpecified",
        // SERVER_GC
        "1"
    };

    size_t property_size = sizeof(property_keys) / sizeof(property_keys[0]);

    // Bind CoreCLR
    if (!coreclr::bind(clr_path))
    {
        trace::error(_X("failed to bind to coreclr"));
        return 1;
    }

    // Verbose logging
    if (trace::is_enabled())
    {
        for (int i = 0; i < property_size; ++i)
        {
            trace::verbose(_X("Property %s = %s"), property_keys[i], property_values[i]);
        }
    }

    std::string own_path;
    pal::to_stdstring(args.own_path.c_str(), &own_path);

    // Initialize CoreCLR
    coreclr::host_handle_t host_handle;
    coreclr::domain_id_t domain_id;
    auto hr = coreclr::initialize(
        own_path.c_str(),
        "clrhost",
        property_keys,
        property_values,
        property_size,
        &host_handle,
        &domain_id);
    if (!SUCCEEDED(hr))
    {
        trace::error(_X("failed to initialize CoreCLR, HRESULT: 0x%X"), hr);
        return 1;
    }

    if (trace::is_enabled())
    {
        pal::string_t arg_str;
        for (int i = 0; i < args.app_argc; i++)
        {
            arg_str.append(args.app_argv[i]);
            arg_str.append(_X(","));
        }
        trace::info(_X("Launch host: %s app: %s, argc: %d args: %s"), args.own_path.c_str(),
            args.managed_application.c_str(), args.app_argc, arg_str.c_str());
    }

    // Initialize with empty strings
    std::vector<std::string> argv_strs(args.app_argc);
    std::vector<const char*> argv(args.app_argc);
    for (int i = 0; i < args.app_argc; i++)
    {
        pal::to_stdstring(args.app_argv[i], &argv_strs[i]);
        argv[i] = argv_strs[i].c_str();
    }

    // Execute the application
    unsigned int exit_code = 1;
    hr = coreclr::execute_assembly(
        host_handle,
        domain_id,
        argv.size(),
        argv.data(),
        pal::to_stdstring(args.managed_application).c_str(),
        &exit_code);
    if (!SUCCEEDED(hr))
    {
        trace::error(_X("failed to execute managed app, HRESULT: 0x%X"), hr);
        return 1;
    }

    // Shut down the CoreCLR
    hr = coreclr::shutdown(host_handle, domain_id);
    if (!SUCCEEDED(hr))
    {
        trace::warning(_X("failed to shut down CoreCLR, HRESULT: 0x%X"), hr);
    }

    coreclr::unload();

    return exit_code;
}

#if defined(_WIN32)
int __cdecl wmain(const int argc, const pal::char_t* argv[])
#else
int main(const int argc, const pal::char_t* argv[])
#endif
{
    // Take care of arguments
    arguments_t args;
    if (!parse_arguments(argc, argv, args))
    {
        return 1;
    }

    // Resolve application path
    if (!pal::realpath(args.managed_application))
    {
        trace::error(_X("failed to locate managed application: %s"), args.managed_application.c_str());
        return 1;
    }

    // Resolve CLR path
    pal::string_t clr_path;
    if (!resolve_clr_path(args, &clr_path))
    {
        trace::error(_X("could not resolve coreclr path"));
        return 1;
    }
    return run(args, clr_path);
}
