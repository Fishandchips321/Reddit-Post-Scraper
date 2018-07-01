using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace redditImageScraper
{
	static class dataDownloader
	{
		private static readonly HttpClient client = new HttpClient();

		public enum postTypes
		{
			text,
			image,
			imgur,
			gif,
			linkOrOther
		}

		public class postData
		{
			public string postTitle;
			public string url;
			public string subreddit;
			public string urlDomain;
			public string author;
			public string postBody;
			public bool nsfw;
			public int commentCount;
			public int timeCreated;
			public int score;
			public int gilded;
			public postTypes type;
		}

		public static postData[] getPosts(string url, int amount = -1)
		{
			List<postData> parsedData = new List<postData>();

			int downloadedPosts = 0;
			string after = "";

			Console.WriteLine($"Downloading post data from {url}");

			do
			{
				JObject downloadedData = downloadPostData(url, after);
				after = getAfter(downloadedData);

				postData[] parsedPartialData = parsePostData(downloadedData);

				downloadedPosts += parsedPartialData.Length;
				//TODO: don't count posts that we don't want (e.g nsfw posts or text posts)
				parsedData.AddRange(parsedPartialData);

			} while (downloadedPosts < amount);

			Console.WriteLine($"Finished downloading data. Downloaded {downloadedPosts} posts");

			return parsedData.ToArray();
		}

		private static JObject downloadPostData(string url, string after = "")
		{
			string completeUrl = "https://www.reddit.com/";
			completeUrl += url;
			completeUrl += "/.json";

			if (after != "")
				completeUrl += $"?after={after}";

			Console.Write($"Retrieving Data part {url}... ");

			//TODO: restart the GET if it times out (the task is cancelled)
			HttpResponseMessage response = client.GetAsync(completeUrl).Result;

			Console.WriteLine("Completed");

			return JObject.Parse(response.Content.ReadAsStringAsync().Result);
		}

		private static postData[] parsePostData(JObject downloadedData)
		{
			if(downloadedData.First == null)
				return new postData[0];

			List<postData> parsedData = new List<postData>();

			foreach (JToken post in downloadedData["data"]["children"].Children())
			{
				if (post == null)
					continue;

				postData parsedPost = new postData
				{
					url = post["data"]["url"].Value<string>(),
					author = post["data"]["author"].Value<string>(),
					commentCount = post["data"]["num_comments"].Value<int>(),
					gilded = post["data"]["gilded"].Value<int>(),
					nsfw = post["data"]["over_18"].Value<bool>(),
					postTitle = post["data"]["title"].Value<string>(),
					score = post["data"]["score"].Value<int>(),
					subreddit = post["data"]["subreddit"].Value<string>(),
					timeCreated = post["data"]["created"].Value<int>(),
					urlDomain = post["data"]["domain"].Value<string>()
				};

				if (parsedPost.url.Contains("reddit.com"))
					parsedPost.postBody = post["data"]["selftext"].Value<string>();

				parsedPost = IDPost(parsedPost);

				parsedData.Add(parsedPost);
			}

			return parsedData.ToArray();
		}

		private static postData IDPost(postData post)
		{
			if (post.url.Contains("i.redd.it") || post.url.Contains("i.imgur")) //If the url can be used directly to download the media
				post.type = postTypes.image;
			else if (post.url.Contains("imgur")) //If the imgur api is needed to get the image/gif
				post.type = postTypes.imgur;
			else if (post.url.Contains("gfycat")) //If the url needs to be modified to download a gfycat gif
				post.type = postTypes.gif;
			else if (post.url.Contains("reddit") && !post.url.Contains("/r/")) //if the url points to a reddit comment thread, it is just a text post
				post.type = postTypes.text;
			else //if the url is something else, it could be just a link to another website (will be ignored/not downloaded)
				post.type = postTypes.linkOrOther;
			return post;
		}

		//get the token used to get the next list of posts (so we don't get the same list each time we send a request to the reddit servers)
		//this is the same as using the "next" button on the reddit website
		private static string getAfter(JObject data)
		{
			return data["data"]["after"].Value<string>();
		}
	}
}
