using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace redditImageScraper
{
	static class redditPostDownloader
	{
		private static readonly HttpClient _client = new HttpClient();
		public const string downloadFolder = "downloads";
		public const int maxFilenameSize = 100;

		public static void downloadUserPosts(string username, int amount = 0, bool nsfw = false, bool text = false)
		{
			string urlPart = $"user/{username}/submitted";

			dataDownloader.postData[] posts;

			if (amount > 0)
				posts = dataDownloader.getPosts(urlPart, amount);
			else
			{
				posts = dataDownloader.getPosts(urlPart);
				amount = posts.Length;
			}

			for (int x = 0; x < amount; x++)
			{
				downloadMedia(posts[x], text);
			}

		}

		public static void downloadSubredditPosts(string subreddit, int amount = 0, bool nsfw = false, bool text = false)
		{
			string urlPart = $"r/{subreddit}";

			dataDownloader.postData[] posts;

			if (amount > 0)
				posts = dataDownloader.getPosts(urlPart, amount);
			else
			{
				posts = dataDownloader.getPosts(urlPart);
				amount = posts.Length;
			}

			for (int x = 0; x < amount; x++)
			{
				if(posts[x].nsfw && nsfw)
					downloadMedia(posts[x], text);
				else if (!posts[x].nsfw)
					downloadMedia(posts[x], text);

			}
		}

		private static void downloadMedia(dataDownloader.postData media, bool downloadText)
		{
			switch (media.type)
			{
				case dataDownloader.postTypes.image:
					Console.WriteLine($"Downloading file {media.url}");
					downloadImage(media);
					break;
				case dataDownloader.postTypes.gif:
					Console.WriteLine($"Downloading file {media.url}");
					downloadGfycatVideo(media);
					break;
				case dataDownloader.postTypes.imgur:
					Console.WriteLine($"Downloading file(s) {media.url}");
					imgurDownloader.downloadImgurFile(media.url, $"{downloadFolder}/{media.subreddit}", media.postTitle);
					break;
				case dataDownloader.postTypes.text:
					if (downloadText)
					{
						Console.WriteLine($"Saving text post {media.postTitle}");
						savePostText(media);
					}
					break;
			}
		}

		private static void savePostText(dataDownloader.postData post)
		{
			if (!Directory.Exists(downloadFolder))
				Directory.CreateDirectory(downloadFolder);

			if (!Directory.Exists($"{downloadFolder}/{post.subreddit}"))
				Directory.CreateDirectory($"{downloadFolder}/{post.subreddit}");

			string finalName = post.postTitle;
			int nextNameNum = 1;

			while (File.Exists($"{downloadFolder}/{post.subreddit}/{finalName}.txt"))
			{
				finalName = post.postTitle + nextNameNum;
				nextNameNum++;
			}

			if (finalName.Length > maxFilenameSize)
				finalName = finalName.Remove(maxFilenameSize);

			finalName = removeIllegalChars(finalName);

			File.WriteAllText($"{downloadFolder}/{post.subreddit}/{finalName}.txt", post.postBody);
		}

		private static void downloadGfycatVideo(dataDownloader.postData gifData)
		{
			string url = gifData.url;
			url = url.Insert("https://".Length, "giant.");
			url += ".mp4";

			HttpResponseMessage response = _client.GetAsync(url).Result;

			if (response.StatusCode != HttpStatusCode.OK)
				return;

			byte[] data = response.Content.ReadAsByteArrayAsync().Result;

			if (!Directory.Exists($"{downloadFolder}/{gifData.subreddit}"))
				Directory.CreateDirectory($"{downloadFolder}/{gifData.subreddit}");

			string finalName = gifData.postTitle;
			int nextNameNum = 1;

			while (File.Exists($"{downloadFolder}/{gifData.subreddit}/{finalName}.mp4"))
			{
				finalName = gifData.postTitle + nextNameNum;
				nextNameNum++;
			}

			if (finalName.Length > maxFilenameSize)
				finalName = finalName.Remove(maxFilenameSize);

			finalName = removeIllegalChars(finalName);

			File.WriteAllBytes($"{downloadFolder}/{gifData.subreddit}/{finalName}.mp4", data);
		}

		private static void downloadImage(dataDownloader.postData imageData)
		{
			if (imageData.url.EndsWith(".gifv"))
			{
				imageData.url = imageData.url.Remove(imageData.url.Length - ".gifv".Length);
				imageData.url += ".mp4";
			}

			HttpResponseMessage response = _client.GetAsync(imageData.url).Result;

			if (response.StatusCode != HttpStatusCode.OK)
				return;

			byte[] data = response.Content.ReadAsByteArrayAsync().Result;

			if (data.Length == 0)
				return;

			if (!Directory.Exists($"{downloadFolder}/{imageData.subreddit}"))
				Directory.CreateDirectory($"{downloadFolder}/{imageData.subreddit}");

			string finalName = imageData.postTitle;
			int nextNameNum = 1;

			while (File.Exists($"{downloadFolder}/{imageData.subreddit}/{finalName}{getFileExtensionFromIRedditUrl(imageData.url)}"))
			{
				finalName = imageData.postTitle + nextNameNum;
				nextNameNum++;
			}

			finalName = removeIllegalChars(finalName);

			if (finalName.Length > maxFilenameSize)
				finalName = finalName.Remove(maxFilenameSize);

			finalName += getFileExtensionFromIRedditUrl(imageData.url);

			File.WriteAllBytes($"{downloadFolder}/{imageData.subreddit}/{finalName}", data);
		}

		private static string getFileExtensionFromIRedditUrl(string fullUrl)
		{
			if (fullUrl.StartsWith("https://i.redd.it/") || fullUrl.StartsWith("https://i.imgur.com/"))
			{
				return fullUrl.Remove(0, fullUrl.LastIndexOf(".", StringComparison.Ordinal));
			}

			if (fullUrl.StartsWith("https://gfycat.com/"))
			{
				return ".mp4";
			}

			return "";
		}

		private static string removeIllegalChars(string filename)
		{
			string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

			foreach (char c in invalid)
			{
				filename = filename.Replace(c.ToString(), "");
			}

			return filename;
		}
	}
}
