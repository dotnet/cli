// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include <thread>
#include <fstream>
#include "pal.h"
#include "utils.h"
#include "trace.h"
#include "breadcrumbs.h"

static const int SYNCHRONOUS_WRITE_SIZE = 20;

breadcrumb_writer_t::breadcrumb_writer_t(const std::unordered_set<pal::string_t>* files)
    : m_status(false)
    , m_files(files)
{
    pal::get_default_breadcrumb_store(&m_breadcrumb_store);
    if (!pal::directory_exists(m_breadcrumb_store))
    {
        m_breadcrumb_store.clear();
    }
}

void breadcrumb_writer_t::begin_write()
{
    if (m_breadcrumb_store.empty())
    {
        m_status = false;
        return;
    }

    if (m_files->size() <= SYNCHRONOUS_WRITE_SIZE)
    {
        m_status = write();
        return;
    }
    m_thread = std::thread(thread_callback, this);
}

bool breadcrumb_writer_t::write()
{
    bool successful = true;
    for (const auto& file : *m_files)
    {
        pal::string_t file_path = m_breadcrumb_store;
        pal::string_t file_name = _X("netcore,") + file;
        append_path(&file_path, file_name.c_str());
        if (!pal::file_exists(file_path))
        {
            if (!pal::touch_file(file_path))
            {
                successful = false;
            }
        }
    }
    return successful;
}

bool breadcrumb_writer_t::thread_callback(breadcrumb_writer_t* p_this)
{
    bool retval = false;
    try
    {
        retval = p_this->write();
    }
    catch (...)
    {
        trace::warning(_X("An unexpected exception was thrown while leaving breadcrumbs"));
    }
    try
    {
        p_this->m_promise.set_value(retval);
    }
    catch (...) { }
    return retval;
}

bool breadcrumb_writer_t::end_write()
{
    if (m_thread.joinable())
    {
        m_thread.join();
        std::future<bool> future = m_promise.get_future();
        m_status = future.get();
    }
    return m_status;
}

