// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <thread>
#include "pal.h"
#include "utils.h"
#include "trace.h"
#include "breadcrumbs.h"

static const int SYNCHRONOUS_WRITE_SIZE = 20;

breadcrumb_writer_t::breadcrumb_writer_t(const std::unordered_set<pal::string_t>* files)
    : m_status(false)
    , m_files(files)
{
    if (!pal::get_default_breadcrumb_store(&m_breadcrumb_store))
    {
        m_breadcrumb_store.clear();
    }
}

// Begin breadcrumb writing: write synchronously or launch a
// thread to write breadcrumbs.
void breadcrumb_writer_t::begin_write()
{
    trace::verbose(_X("--- Begin breadcrumb write"));
    if (m_breadcrumb_store.empty())
    {
        trace::verbose(_X("Breadcrumb store was not obtained... skipping write."));
        m_status = false;
        return;
    }

    trace::verbose(_X("Number of breadcrumb files to write is %d"), m_files->size());
    if (m_files->size() <= SYNCHRONOUS_WRITE_SIZE)
    {
        trace::verbose(_X("Writing breadcrumbs on the calling thread"));
        m_status = write();
        return;
    }
    m_thread = std::thread(thread_callback, this);
    trace::verbose(_X("Breadcrumbs will be written using a background thread"));
}

// Write the breadcrumbs. This method could be called fom the
// calling thread and the background thread. But at any given
// time only 1 thread will be here.
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

// ThreadProc for the background writer.
bool breadcrumb_writer_t::thread_callback(breadcrumb_writer_t* p_this)
{
    bool retval = false;
    try
    {
        trace::verbose(_X("Breadcrumb thread write callback..."));
        retval = p_this->write();
    }
    catch (...)
    {
        trace::warning(_X("An unexpected exception was thrown while leaving breadcrumbs"));
    }
    try
    {
        // Signal the waiting thread.
        p_this->m_promise.set_value(retval);
    }
    catch (...)
    {
        trace::warning(_X("Could not signal waiting threads..."));
    }
    return retval;
}

// Wait for completion of the background tasks, if any.
bool breadcrumb_writer_t::end_write()
{
    if (m_thread.joinable())
    {
        trace::verbose(_X("Waiting for breadcrumb thread to exit..."));
        m_thread.join();
        std::future<bool> future = m_promise.get_future();
        m_status = future.get();
    }
    trace::verbose(_X("--- End breadcrumb write %d"), m_status);
    return m_status;
}

