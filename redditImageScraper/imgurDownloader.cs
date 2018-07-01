using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace redditImageScraper
{
	static class imgurDownloader
	{
		private const string clientID = "84e71a9f61158c2", clientSecret = "f106282a689fef34b47d59c8942b75aa61ccd802";

		private static readonly HttpClient client = new HttpClient();

		public static void init()
		{
			client.DefaultRequestHeaders.Add("Authorization", $"Client-ID {clientID}");
		}

		public static void downloadImgurFile(string url, string path, string filename)
		{
			if (url.Contains("gallery")) //if the url is a gallery (a collection of one or more imgur images)
			{
				downloadImgurAlbum(url, path, filename);
				return;
			}
			
			//if the url is a single imgur image
			downloadFile(getFileURL(url), path, filename);
		}

		public static void downloadImgurAlbum(string url, string path, string fileName)
		{
			foreach (string image in getAlbumImages(url))
			{
				downloadFile(image, path, fileName);
			}
		}

		private static string[] getAlbumImages(string url)
		{
			string albumHash = url.Remove(0, url.LastIndexOf("/", StringComparison.Ordinal) + 1);

			Console.Write("Downloading album metadata... ");

			HttpResponseMessage response = client.GetAsync($"https://api.imgur.com/3/album/{albumHash}/images").Result;

			Console.WriteLine("Complete");

			string rawData = response.Content.ReadAsStringAsync().Result;

			JObject data = JObject.Parse(response.Content.ReadAsStringAsync().Result);

			if (data.First == null)
				return new string[0];

			List<string> fileUrls = new List<string>();

			foreach (JToken file in data["data"])
			{
				fileUrls.Add(file["link"].Value<string>());
			}

			return fileUrls.ToArray();
		}

		private static string getFileURL(string url)
		{
			string fileHash = url.Remove(0, url.LastIndexOf("/", StringComparison.Ordinal) + 1);

			Console.Write("Downloading image metadata... ");

			HttpResponseMessage response = client.GetAsync($"https://api.imgur.com/3/image/{fileHash}").Result;

			Console.WriteLine("Complete");

			string rawData = response.Content.ReadAsStringAsync().Result;

			JObject data = JObject.Parse(response.Content.ReadAsStringAsync().Result);

			return data["data"]["link"].Value<string>();
		}

		private static void downloadFile(string url, string path, string filename)
		{
			HttpResponseMessage response = client.GetAsync(url).Result;

			byte[] data = response.Content.ReadAsByteArrayAsync().Result;

			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			filename = removeIllegalChars(filename);

			string finalName = filename;
			int nextNameNum = 1;

			while (File.Exists($"{path}/{finalName}{getFileExtensionFromUrl(url)}"))
			{
				finalName = filename + nextNameNum;
				nextNameNum++;
			}

			if (finalName.Length > redditPostDownloader.maxFilenameSize)
				finalName = finalName.Remove(redditPostDownloader.maxFilenameSize);

			finalName += getFileExtensionFromUrl(url);

			File.WriteAllBytes($"{path}/{finalName}", data);
		}

		private static string getFileExtensionFromUrl(string url)
		{
			return url.Remove(0, url.LastIndexOf(".", StringComparison.Ordinal));
		}

		private static string removeIllegalChars(string filename)
		{
			string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

			foreach (char c in invalid)
			{
				filename = filename.Replace(c.ToString(), "¦");
			}

			return filename;
		}
	}
}
