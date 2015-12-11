// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef ARGS_H
#define ARGS_H

#include "utils.h"
#include "pal.h"
#include "trace.h"

struct arguments_t
{
    pal::string_t own_path;
    pal::string_t app_dir;
    pal::string_t svc_dir;
    pal::string_t runtime_svc_dir;
    pal::string_t home_dir;
    pal::string_t managed_application;

    int app_argc;
    const pal::char_t** app_argv;

    arguments_t();
};

bool parse_arguments(const int argc, const pal::char_t* argv[], arguments_t& args);

#endif // ARGS_H
