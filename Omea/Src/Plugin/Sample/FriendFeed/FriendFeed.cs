// SPDX-FileCopyrightText: 2008 FriendFeed
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;

namespace FriendFeed {
  /// <summary>
  /// A client to interact with the FriendFeed API.
  ///
  /// More information about the API is available at http://friendfeed.com/api/.
  /// </summary>
  class FriendFeedClient {
    private string nickname_;
    private string remoteKey_;

    /// <summary>
    /// Creates an un-authenticated FriendFeed API client.
    ///
    /// An un-authenticated client can only perform read operations on public
    /// feeds.
    /// </summary>
    public FriendFeedClient() {
    }

    /// <summary>
    /// Creates a FriendFeed API client authenticated with the given credentials.
    /// </summary>
    public FriendFeedClient(string nickname, string remoteKey) {
      nickname_ = nickname;
      remoteKey_ = remoteKey;
    }

    /// <summary>
    /// Publishes the given message to the authenticated user's feed.
    /// </summary>
    /// <param name="message">The text of the message</param>
    /// <returns>The new entry as returned by the server</returns>
    public Entry PublishMessage(string message) {
      return PublishLink(message, null);
    }

    /// <summary>
    /// Publishes the given link to the authenticated user's feed.
    /// </summary>
    /// <param name="title">The title of the link</param>
    /// <param name="link">The link URL</param>
    /// <returns>The new entry as returned by the server</returns>
    public Entry PublishLink(string title, string link) {
      return PublishLink(title, link, null);
    }

    /// <summary>
    /// Publishes the given link to the authenticated user's feed.
    /// </summary>
    /// <param name="title">The title of the link</param>
    /// <param name="link">The link URL</param>
    /// <param name="comment">The initial comment for this entry</param>
    /// <returns>The new entry as returned by the server</returns>
    public Entry PublishLink(string title, string link, string comment) {
      return PublishLink(title, link, comment, null);
    }

    /// <summary>
    /// Publishes the given link to the authenticated user's feed.
    /// </summary>
    /// <param name="title">The title of the link</param>
    /// <param name="link">The link URL</param>
    /// <param name="comment">The initial comment for this entry</param>
    /// <param name="imageUrls">URLs of the thumbnails to be included with this entry</param>
    /// <returns>The new entry as returned by the server</returns>
    public Entry PublishLink(string title, string link, string comment, ThumbnailUrl[] imageUrls) {
      return PublishLink(title, link, comment, imageUrls, null);
    }

    /// <summary>
    /// Publishes the given link to the authenticated user's feed.
    /// </summary>
    /// <param name="title">The title of the link</param>
    /// <param name="link">The link URL</param>
    /// <param name="comment">The initial comment for this entry</param>
    /// <param name="imageUrls">URLs of the thumbnails to be included with this entry</param>
    /// <param name="imagePaths">Paths to local image files to be included as thumbnails with this entry</param>
    /// <returns>The new entry as returned by the server</returns>
    public Entry PublishLink(string title, string link, string comment, ThumbnailUrl[] imageUrls, ThumbnailFile[] imageFiles) {
      SortedDictionary<string, string> postArguments = new SortedDictionary<string, string>();
      postArguments["title"] = title;
      if (link != null) {
        postArguments["link"] = link;
      }
      if (comment != null) {
        postArguments["comment"] = comment;
      }
      if (imageUrls != null) {
        for (int i = 0; i < imageUrls.Length; i++) {
          postArguments["image" + i + "_url"] = imageUrls[i].Url;
          if (imageUrls[i].Link != null) {
            postArguments["image" + i + "_link"] = imageUrls[i].Url;
          }
        }
      }
      SortedDictionary<string, string> fileAttachments = new SortedDictionary<string, string>();
      if (imageFiles != null) {
        for (int i = 0; i < imageFiles.Length; i++) {
          fileAttachments["file" + i] = imageFiles[i].Path;
          if (imageFiles[i].Link != null) {
            postArguments["file" + i + "_link"] = imageUrls[i].Url;
          }
        }
      }
      HttpWebResponse response = MakeRequest("/api/share", null, postArguments, fileAttachments);
      XmlDocument document = new XmlDocument();
      document.Load(response.GetResponseStream());
      return (new Feed(document.DocumentElement)).Entries[0];
    }

    /// <summary>
    /// Returns the most recent entries from all publicly visible users.
    /// </summary>
    public Feed FetchPublicFeed() {
      return FetchPublicFeed(null);
    }
    public Feed FetchPublicFeed(string service) {
      return FetchPublicFeed(null, 0);
    }
    public Feed FetchPublicFeed(string service, int start) {
      return FetchPublicFeed(null, 0, 30);
    }
    public Feed FetchPublicFeed(string service, int start, int num) {
      return FetchFeed("/api/feed/public", service, start, num);
    }

    /// <summary>
    /// Returns the most recent entries from the authenticated user's
    /// subscriptions, as they would see on their FriendFeed home page.
    /// </summary>
    public Feed FetchHomeFeed() {
      return FetchHomeFeed(null);
    }
    public Feed FetchHomeFeed(string service) {
      return FetchHomeFeed(null, 0);
    }
    public Feed FetchHomeFeed(string service, int start) {
      return FetchHomeFeed(null, 0, 30);
    }
    public Feed FetchHomeFeed(string service, int start, int num) {
      return FetchFeed("/api/feed/home", service, start, num);
    }

    /// <summary>
    /// Fetches the most recent entries for the user with the given nickname.
    ///
    /// If the user has a private feed, authentication is required.
    /// </summary>
    public Feed FetchUserFeed(string nickname) {
      return FetchUserFeed(nickname, null);
    }
    public Feed FetchUserFeed(string nickname, string service) {
      return FetchUserFeed(nickname, null, 0);
    }
    public Feed FetchUserFeed(string nickname, string service, int start) {
      return FetchUserFeed(nickname, null, 0, 30);
    }
    public Feed FetchUserFeed(string nickname, string service, int start, int num) {
      return FetchFeed("/api/feed/user/" + HttpUtility.UrlEncode(nickname), service, start, num);
    }

    /// <summary>
    /// Fetches the most recent entries for the given list of users.
    ///
    /// If any of the users has a private feed, authentication is required.
    /// </summary>
    public Feed FetchMultiUserFeed(string[] nicknames) {
      return FetchMultiUserFeed(nicknames, null);
    }
    public Feed FetchMultiUserFeed(string[] nicknames, string service) {
      return FetchMultiUserFeed(nicknames, null, 0);
    }
    public Feed FetchMultiUserFeed(string[] nicknames, string service, int start) {
      return FetchMultiUserFeed(nicknames, null, 0, 30);
    }
    public Feed FetchMultiUserFeed(string[] nicknames, string service, int start, int num) {
      SortedDictionary<string, string> urlArguments = new SortedDictionary<string, string>();
      urlArguments["nickname"] = string.Join(",", nicknames);
      return FetchFeed("/api/feed/user", service, start, num, urlArguments);
    }

    /// <summary>
    /// Fetches the feed at the given path, parsing and returning the result.
    /// </summary>
    public Feed FetchFeed(string path, string service, int start, int num) {
      return FetchFeed(path, service, start, num, null);
    }

    /// <summary>
    /// Fetches the feed at the given path with the given URL arguments.
    /// </summary>
    public Feed FetchFeed(string path, string service, int start, int num, SortedDictionary<string, string> urlArguments) {
      if (urlArguments == null) urlArguments = new SortedDictionary<string, string>();
      if (service != null) urlArguments["service"] = service;
      urlArguments["start"] = start.ToString();
      urlArguments["num"] = num.ToString();
      HttpWebResponse response = MakeRequest(path, urlArguments, null, null);
      XmlDocument document = new XmlDocument();
      document.Load(response.GetResponseStream());
      return new Feed(document.DocumentElement);
    }

    /// <summary>
    /// Makes an HTTP request to the FriendFeed servers.
    ///
    /// If this client was created with a nickname and remote key, the request
    /// is automatically authenticated. If postArguments is given, the request
    /// will be a POST request. We send a GET otherwise.
    /// </summary>
    /// <param name="path">The path for the request, e.g., /api/feed/home</param>
    /// <param name="urlArguments">The arguments to be included in the URL, e.g., {"start": "0"}</param>
    /// <param name="postArguments">The arguments to be included in the POST body of the request</param>
    /// <param name="fileAttachments">Files to be uploaded with this request</param>
    public HttpWebResponse MakeRequest(string path, SortedDictionary<string, string> urlArguments, SortedDictionary<string, string> postArguments, SortedDictionary<string, string> fileAttachments) {
      if (urlArguments == null) urlArguments = new SortedDictionary<string, string>();
      urlArguments["format"] = "xml";
      string url = "http://friendfeed.com" + path + "?" + UrlEncode(urlArguments);
      HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);

      // Encode the POST body if POST args are given
      if (postArguments != null) {
        request.Method = "POST";
        string boundary = Guid.NewGuid().ToString().Replace("-", "");
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        Stream stream = request.GetRequestStream();
        foreach (KeyValuePair<string, string> pair in postArguments) {
          byte[] value = Encoding.UTF8.GetBytes(string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n", boundary, pair.Key, pair.Value));
          stream.Write(value, 0, value.Length);
        }
        foreach (KeyValuePair<string, string> pair in fileAttachments) {
          FileInfo info = new FileInfo(pair.Value);
          byte[] contents = new byte[info.Length];
          using (FileStream file = File.OpenRead(pair.Value)) {
            file.Read(contents, 0, contents.Length);
          }
          byte[] value = Encoding.UTF8.GetBytes(string.Format("--{0}\r\nContent-Disposition: file; name=\"{1}\"; filename=\"{2}\"\r\n\r\n", boundary, pair.Key, Path.GetFileName(pair.Value)));
          stream.Write(value, 0, value.Length);
          stream.Write(contents, 0, contents.Length);
          stream.Write(Encoding.UTF8.GetBytes("\r\n"), 0, 2);
        }
        byte[] end = Encoding.UTF8.GetBytes(string.Format("--{0}--\r\n", boundary));
        stream.Write(end, 0, end.Length);
        stream.Close();
      } else {
        request.Method = "GET";
      }

      // Add the HTTP Basic auth header if we have credentials
      if (nickname_ != null && remoteKey_ != null) {
        CredentialCache cache = new CredentialCache();
        cache.Add(new Uri("http://friendfeed.com/api/"), "Basic", new NetworkCredential(nickname_, remoteKey_));
        request.Credentials = cache;
      }

      System.Diagnostics.Debug.WriteLine("Downloading " + url);
      request.UserAgent = "FriendFeedCSharpApi/0.4";
      return (HttpWebResponse) request.GetResponse();
    }

    private string UrlEncode(SortedDictionary<string, string> arguments) {
      string[] parts = new string[arguments.Count];
      int i = 0;
      foreach (KeyValuePair<string, string> pair in arguments) {
        parts[i++] = HttpUtility.UrlEncode(pair.Key) + "=" + HttpUtility.UrlEncode(pair.Value);
      }
      return string.Join("&", parts);
    }
  }

  class ThumbnailUrl {
    public string Url;
    public string Link;

    /// <summary>
    /// Creates an entry thumbnail from the image at the given URL.
    ///
    /// The thumbnail will link to the main entry link. If you want the
    /// thumbnail to link to a different URL, you can specify a link in
    /// the other constructor.
    /// </summary>
    public ThumbnailUrl(string url) {
      this.Url = url;
    }

    /// <summary>
    /// Creates an entry thumbnail from the image at the given URL.
    ///
    /// The thumbnail image will link to the given link URL.
    /// </summary>
    public ThumbnailUrl(string url, string link) {
      this.Url = url;
      this.Link = link;
    }
  }

  class ThumbnailFile {
    public string Path;
    public string Link;

    /// <summary>
    /// Creates an entry thumbnail from the file at the given path.
    ///
    /// The thumbnail will link to the main entry link. If you want the
    /// thumbnail to link to a different URL, you can specify a link in
    /// the other constructor.
    /// </summary>
    public ThumbnailFile(string path) {
      this.Path = path;
    }

    /// <summary>
    /// Creates an entry thumbnail from the file at the given path.
    ///
    /// The thumbnail image will link to the given link URL.
    /// </summary>
    public ThumbnailFile(string path, string link) {
      this.Path = path;
      this.Link = link;
    }
  }

  class Feed {
    public List<Entry> Entries;

    public Feed(XmlElement element) {
      Entries = new List<Entry>();
      foreach (XmlElement child in element.ChildNodes) {
        Entries.Add(new Entry(child));
      }
    }
  }

  class Entry {
    public string Id;
    public string Title;
    public string Link;
    public DateTime Published;
    public DateTime Updated;
    public User User;
    public Service Service;
    public List<Comment> Comments;
    public List<Like> Likes;
    public List<Media> Media;

    public Entry(XmlElement element) {
      Id = Util.ChildValue(element, "id");
      Title = Util.ChildValue(element, "title");
      Link = Util.ChildValue(element, "link");
      Published = DateTime.Parse(Util.ChildValue(element, "published"));
      Updated = DateTime.Parse(Util.ChildValue(element, "updated"));
      User = new User(Util.ChildElement(element, "user"));
      Service = new Service(Util.ChildElement(element, "user"));
      Comments = new List<Comment>();
      foreach (XmlElement child in element.GetElementsByTagName("comment")) {
        Comments.Add(new Comment(child));
      }
      Likes = new List<Like>();
      foreach (XmlElement child in element.GetElementsByTagName("like")) {
        Likes.Add(new Like(child));
      }
      Media = new List<Media>();
      foreach (XmlElement child in element.GetElementsByTagName("media")) {
        Media.Add(new Media(child));
      }
    }
  }

  class User {
    public string Id;
    public string Nickname;
    public string ProfileUrl;

    public User(XmlElement element) {
      Id = Util.ChildValue(element, "id");
      Nickname = Util.ChildValue(element, "nickname");
      ProfileUrl = Util.ChildValue(element, "profileUrl");
    }
  }

  class Service {
    public string Id;
    public string Name;
    public string IconUrl;
    public string ProfileUrl;

    public Service(XmlElement element) {
      Id = Util.ChildValue(element, "id");
      Name = Util.ChildValue(element, "name");
      IconUrl = Util.ChildValue(element, "iconUrl");
      ProfileUrl = Util.ChildValue(element, "profileUrl");
    }
  }

  class Like {
    public DateTime Date;
    public User User;

    public Like(XmlElement element) {
      Date = DateTime.Parse(Util.ChildValue(element, "date"));
      User = new User(Util.ChildElement(element, "user"));
    }
  }

  class Comment {
    public DateTime Date;
    public User User;
    public string Body;

    public Comment(XmlElement element) {
      Date = DateTime.Parse(Util.ChildValue(element, "date"));
      User = new User(Util.ChildElement(element, "user"));
      Body = Util.ChildValue(element, "body");
    }
  }

  class Media {
    public string Title;
    public string Player;
    public List<Thumbnail> Thumbnails;
    public List<Content> Content;

    public Media(XmlElement element) {
      Title = Util.ChildValue(element, "title");
      Player = Util.ChildValue(element, "player");
      Thumbnails = new List<Thumbnail>();
      foreach (XmlElement child in element.GetElementsByTagName("thumbnail")) {
        Thumbnails.Add(new Thumbnail(child));
      }
      Content = new List<Content>();
      foreach (XmlElement child in element.GetElementsByTagName("content")) {
        Content.Add(new Content(child));
      }
    }
  }

  class Thumbnail {
    public string Url;
    public string Width;
    public string Height;

    public Thumbnail(XmlElement element) {
      Url = Util.ChildValue(element, "url");
      Width = Util.ChildValue(element, "width");
      Height = Util.ChildValue(element, "height");
    }
  }

  class Content {
    public string Url;
    public string Type;
    public string Width;
    public string Height;

    public Content(XmlElement element) {
      Url = Util.ChildValue(element, "url");
      Type = Util.ChildValue(element, "type");
      Width = Util.ChildValue(element, "width");
      Height = Util.ChildValue(element, "height");
    }
  }

  static internal class Util {
    public static XmlElement ChildElement(XmlElement element, string name) {
      XmlNodeList list = element.GetElementsByTagName(name);
      foreach (XmlElement child in list) {
        if (child.ParentNode == element) {
          return child;
        }
      }
      return null;
    }

    public static string ChildValue(XmlElement element, string name) {
      XmlElement child = ChildElement(element, name);
      return (child == null) ? null : child.InnerText;
    }
  }
}
