/* GLIB - Library of useful routines for C programming
 * Copyright (C) 1995-1997  Peter Mattis, Spencer Kimball and Josh MacDonald
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License as published by the Free Software Foundation; either
 * version 2 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.	 See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License along with this library; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

/*
 * Modified by the GLib Team and others 1997-1999.  See the AUTHORS
 * file for a list of people on the GLib Team.  See the ChangeLog
 * files for a list of changes.  These files are distributed with
 * GLib at ftp://ftp.gtk.org/pub/gtk/. 
 */


#include <stdlib.h>
#include <stdarg.h>
#include <stdio.h>
#include <string.h>

#ifdef HAVE_CONFIG_H
#include "config.h"
#endif

#include "glib.h"

#ifdef _WIN32_
#ifndef _WINDOWS_
#include <windows.h>
#endif
static void
ensure_stdout_valid (void)
{
  HANDLE handle;

  handle = GetStdHandle (STD_OUTPUT_HANDLE);
  
  if (handle == INVALID_HANDLE_VALUE)
    {
      AllocConsole ();
      freopen ("CONOUT$", "w", stdout);
    }
}
#else
#define ensure_stdout_valid()	/* Define as empty */
#endif

void
g_print (const gchar *format,
	 ...)
{
  va_list args;
  gchar *string;

  g_return_if_fail (format != NULL);
  
  va_start (args, format);
  string = g_strdup_vprintf (format, args);
  va_end (args);
  
  ensure_stdout_valid ();
  fputs (string, stdout);
  fflush (stdout);

  g_free (string);
}

void
g_printerr (const gchar *format,
	    ...)
{
  va_list args;
  gchar *string;
  
  g_return_if_fail (format != NULL);
  
  va_start (args, format);
  string = g_strdup_vprintf (format, args);
  va_end (args);
  
  fputs (string, stderr);
  fflush (stderr);

  g_free (string);
}

void
g_messages_init (void)
{
}

void
g_error (const gchar *format,
	 ...)
{
  va_list args;
  va_start (args, format);
  g_printerr (format, args);
  va_end (args);
}

void
g_message (const gchar *format,
	   ...)
{
  va_list args;
  va_start (args, format);
  g_print (format, args);
  va_end (args);
}

void
g_warning (const gchar *format,
	   ...)
{
  va_list args;
  va_start (args, format);
  g_printerr (format, args);
  va_end (args);
}
