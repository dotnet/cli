#include "args.h"
#include "utils.h"

arguments_t::arguments_t() :
    trace_level(trace::level_t::Error),
    managed_application(_X("")),
    clr_path(_X("")),
    app_argc(0),
    app_argv(nullptr),
    app_base(_X(""))
{
}

void display_help()
{
    xerr <<
        _X("Usage: " HOST_EXE_NAME " [OPTIONS] [ASSEMBLY] [ARGUMENTS]\n")
        _X("Execute the specified managed assembly with the passed in arguments\n\n")
        _X("Options:\n")
        _X(" -c,--clr-path <PATH>   Set the directory which contains the CoreCLR runtime. Overrides all other values for CLR search paths\n")
        _X(" -a,--app-base <PATH>   Set the application base path\n")
        _X(" -t,--trace <LEVEL>     Set the trace level emitted by corehost (0 = Errors only (default), 1 = Warnings, 2 = Info, 3 = Verbose)\n");
        _X("\n")
        _X("The Host's behavior can also be altered using the following environment variables:\n")
        _X(" COREHOST_CLR_PATH      Set the directory which contains the CoreCLR runtime. Overrides default search paths, but NOT the -c option\n")
        _X(" COREHOST_TRACE         Set the trace level emitted by corehost (0 = Errors only (default), 1 = Warnings, 2 = Info, 3 = Verbose)\n");
}

bool is_arg(const pal::char_t* value, const pal::char_t* short_name, const pal::char_t* long_name)
{
    return pal::strcmp(value, short_name) == 0 || pal::strcmp(value, long_name) == 0;
}

int parse_command_line(const int argc, const pal::char_t* argv[], arguments_t& args, pal::string_t& trace_string)
{
    for (int i = 1; i < argc; i++)
    {
        if (is_arg(argv[i], _X("-c"), _X("--clr-path")))
        {
            i++;
            if (i >= argc)
            {
                trace::error(_X("argument `--clr-path` must have a value"));
                return -1;
            }
            args.clr_path.assign(argv[i]);
        }
        else if(is_arg(argv[i], _X("-a"), _X("--app-base")))
        {
            i++;
            if (i >= argc)
            {
                trace::error(_X("argument `--app-base` must have a value"));
                return -1;
            }
            args.app_base.assign(argv[i]);
        }
        else if(is_arg(argv[i], _X("-t"), _X("--trace")))
        {
            i++;
            if (i >= argc)
            {
                trace::error(_X("argument `--app-base` must have a value"));
                return -1;
            }
            trace_string.assign(argv[i]);
        }
        else if(argv[i][0] == '-')
        {
            // Some unknown option
            trace::error(_X("unknown option: `%s`"), argv[i]);
            return -1;
        }
        else
        {
            // End of options! i is the managed application
            args.managed_application.assign(argv[i]);

            // i + 1 is the first managed application arg
            return i + 1;
        }
    }

    // Reached the end without a managed application
    trace::error(_X("missing path to the managed application to run"));
    return -1;
}

bool parse_arguments(const int argc, const pal::char_t* argv[], arguments_t& args)
{
    // Get the full name of the application
    if (!pal::get_own_executable_path(args.own_path) || !pal::realpath(args.own_path))
    {
        trace::error(_X("failed to locate current executable"));
        return false;
    }

    // Read environment variables
    pal::string_t trace_str;
    pal::getenv(_X("COREHOST_TRACE"), trace_str);
    pal::getenv(_X("COREHOST_CLR_PATH"), args.clr_path);

    auto own_name = get_filename(args.own_path);
    auto own_dir = get_directory(args.own_path);

    if (own_name.compare(HOST_EXE_NAME) == 0)
    {
        // The host exe is named corehost, which means we support command-line args and
        // expect the managed app to be one
        auto first_app_arg = parse_command_line(argc, argv, args, trace_str);
        if (first_app_arg < 0)
        {
            display_help();
            return false;
        }
        args.app_argc = argc - first_app_arg;
        args.app_argv = &argv[first_app_arg];
    }
    else
    {
        // coreconsole mode. Find the managed app in the same directory
        pal::string_t managed_app(own_dir);
        managed_app.push_back(DIR_SEPARATOR);
        managed_app.append(get_executable(own_name));
        managed_app.append(_X(".dll"));
        args.managed_application = managed_app;
        args.app_argv = &argv[1];
        args.app_argc = argc - 1;
    }

    if (!args.clr_path.empty() && !pal::realpath(args.clr_path))
    {
        trace::error(_X("clr path `%s` could not be located"), args.clr_path.c_str());
        return -1;
    }

    if (!args.app_base.empty() && !pal::realpath(args.app_base))
    {
        trace::error(_X("app base `%s` could not be located"), args.app_base.c_str());
        return -1;
    }

    if (!trace_str.empty())
    {
        auto trace_val = pal::xtoi(trace_str.c_str());
        if (trace_val >= (int)trace::level_t::Error && trace_val <= (int)trace::level_t::Verbose)
        {
            args.trace_level = (trace::level_t)trace_val;
        }
    }

    return true;
}
