#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"
#include "ms-ole.h"
#include "oledecod.h"

pps_entry *stream_tree = NULL;

int
wvOLEDecode (wvParseStruct * ps, char *path, wvStream ** mainfd, wvStream ** tablefd0,
	     wvStream ** tablefd1, wvStream ** data, wvStream ** summary)
{
    MsOle *ole_file = NULL;
    int result = 5;

    if (ms_ole_open (&ole_file, path) == MS_OLE_ERR_OK)
      {
	  MsOleStream **temp_stream;
	  temp_stream = (MsOleStream **) wvMalloc (sizeof (MsOleStream *));

	  ps->ole_file = ole_file;

	  wvTrace (("Opened VFS\n"));
	  if (ms_ole_stream_open
	      (temp_stream, ole_file, "/", "WordDocument",
	       'r') != MS_OLE_ERR_OK)
	    {
		*mainfd = NULL;
		wvTrace (("Opening \"WordDocument\" stream\n"));
	    }
	  else
	    {
		wvTrace (("Opened \"WordDocument\" stream\n"));
		wvStream_libole2_create (mainfd, *temp_stream);
	    }
	  if (ms_ole_stream_open (temp_stream, ole_file, "/", "1Table", 'r')
	      != MS_OLE_ERR_OK)
	    {
		*tablefd1 = NULL;
		wvTrace (("Opening \"1Table\" stream\n"));
	    }
	  else
	    {
		wvTrace (("Opened \"1Table\" stream\n"));
		wvStream_libole2_create (tablefd1, *temp_stream);
	    }
	  if (ms_ole_stream_open (temp_stream, ole_file, "/", "0Table", 'r')
	      != MS_OLE_ERR_OK)
	    {
		*tablefd0 = NULL;
		wvTrace (("Opening \"0Table\" stream\n"));
	    }
	  else
	    {
		wvTrace (("Opened \"0Table\" stream\n"));
		wvStream_libole2_create (tablefd0, *temp_stream);
	    }
	  if (ms_ole_stream_open (temp_stream, ole_file, "/", "Data", 'r') !=
	      MS_OLE_ERR_OK)
	    {
		*data = NULL;
		wvTrace (("Opening \"Data\" stream\n"));
	    }
	  else
	    {
		wvTrace (("Opened \"Data\" stream\n"));
		wvStream_libole2_create (data, *temp_stream);
	    }
	  if (ms_ole_stream_open
	      (temp_stream, ole_file, "/", "\005SummaryInformation",
	       'r') != MS_OLE_ERR_OK)
	    {
		*summary = NULL;
		wvTrace (("Opening \"\\005SummaryInformation\" stream\n"));
	    }
	  else
	    {
		wvTrace (("Opened \"\\005SummaryInformation\" stream\n"));
		wvStream_libole2_create (summary, *temp_stream);
	    }
	  wvFree (temp_stream);
	  result = 0;
      }
#if 0
    else
      {

	  /* We haven't managed to get LibOLE2 to open the file, so we'll try the
	   * old routines (it may be a pre-OLE document).
	   */
	  U32 stream;
	  U32 root_stream;
	  FILE *input;
	  input = fopen (path, "rb");

	  wvTrace (
		   ("LibOLE2 failed to open VFS, falling back on 'old' FILE* methods.\n"));

	  if (input == NULL)
	    {
		wvTrace (("Cannot open file!\n"));
		return 1;
	    }

	  result = OLEdecode (input, &stream_tree, &root_stream, 1);
	  if (result == 0)
	    {
		FILE *temp_file;
		for (stream = stream_tree[root_stream].dir;
		     stream != 0xffffffff; stream = stream_tree[stream].next)
		  {
		      if (stream_tree[stream].type != 1
			  && stream_tree[stream].level == 1)
			{
			    if (!
				(strcmp
				 (stream_tree[stream].name, "WordDocument")))
			      {
				  temp_file =
				      fopen (stream_tree[stream].filename,
					     "rb");
				  if (temp_file == NULL)
				    {
					*mainfd = NULL;
					wvTrace (
						 ("Opening \"WordDocument\" stream\n"));
				    }
				  else
				    {
					wvTrace (
						 ("Opened \"WordDocument\" stream\n"));
					wvStream_FILE_create (mainfd,
							      temp_file);
				    }
			      }
			    else
				if (!
				    (strcmp
				     (stream_tree[stream].name, "1Table")))
			      {
				  temp_file =
				      fopen (stream_tree[stream].filename,
					     "rb");
				  if (temp_file == NULL)
				    {
					*tablefd1 = NULL;
					wvTrace (
						 ("Opening \"1Table\" stream\n"));
				    }
				  else
				    {
					wvTrace (
						 ("Opened \"1Table\" stream\n"));
					wvStream_FILE_create (tablefd1,
							      temp_file);
				    }
			      }
			    else
				if (!
				    (strcmp
				     (stream_tree[stream].name, "0Table")))
			      {
				  temp_file =
				      fopen (stream_tree[stream].filename,
					     "rb");
				  if (temp_file == NULL)
				    {
					*tablefd0 = NULL;
					wvTrace (
						 ("Opening \"0Table\" stream\n"));
				    }
				  else
				    {
					wvTrace (
						 ("Opened \"0Table\" stream\n"));
					wvStream_FILE_create (tablefd0,
							      temp_file);
				    }
			      }
			    else
				if (!
				    (strcmp (stream_tree[stream].name, "Data")))
			      {
				  temp_file =
				      fopen (stream_tree[stream].filename,
					     "rb");
				  if (temp_file == NULL)
				    {
					*data = NULL;
					wvTrace (("Opening \"Data\" stream\n"));
				    }
				  else
				    {
					wvTrace (("Opened \"Data\" stream\n"));
					wvStream_FILE_create (data, temp_file);
				    }
			      }
			    else
				if (!
				    (strcmp
				     (stream_tree[stream].name,
				      "\005SummaryInformation")))
			      {
				  temp_file =
				      fopen (stream_tree[stream].filename,
					     "rb");
				  if (temp_file == NULL)
				    {
					*summary = NULL;
					wvTrace (
						 ("Opening \"\\005SummaryInformation\" stream\n"));
				    }
				  else
				    {
					wvTrace (
						 ("Opened \"\\005SummaryInformation\" stream\n"));
					wvStream_FILE_create (summary,
							      temp_file);
				    }
			      }
			}
		  }
	    }
	  switch (result)
	    {
	    case 5:
		wvTrace (
			 ("File appears to be corrupt, unable to extract streams\n"));
		break;
	    }
      }
#endif

    return (result);
}


/* TODO: Fix up the routines below - at the moment, they work /only/ with the old 
 * TODO: style streams.
 */
pps_entry *
myfind (char *idname, U32 start_entry)
{
    pps_entry *ret = NULL;
    U32 entry;
    for (entry = start_entry; entry != 0xffffffffUL;
	 entry = stream_tree[entry].next)
      {
	  wvTrace (("%s %s\n", stream_tree[entry].name, idname));
	  if (!(strcmp (idname, stream_tree[entry].name)))
	      return (&(stream_tree[entry]));
	  if (stream_tree[entry].type == 2)
	    {
		wvTrace (
			 ("FILE %02lx %5ld %s\n",
			  stream_tree[entry].ppsnumber,
			  stream_tree[entry].size, stream_tree[entry].name));
	    }
	  else
	    {
		wvTrace (
			 ("DIR  %02lx %s\n", stream_tree[entry].ppsnumber,
			  stream_tree[entry].name));
		ret = myfind (idname, stream_tree[entry].dir);
		if (ret)
		    return (ret);
	    }
      }
    return (ret);
}

pps_entry *
wvFindObject (S32 id)
{
    char idname[64];
    sprintf (idname, "_%d", id);
    return (myfind (idname, 0));
}
