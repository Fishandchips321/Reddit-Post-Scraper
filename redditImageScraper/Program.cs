using System;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;

namespace redditImageScraper
{
	class Program
	{
		static void Main(string[] args)
		{
			imgurDownloader.init();
			bool repeat;

			do
			{
				Console.WriteLine("What do you want to download?");
				Console.WriteLine("1: User posts");
				Console.WriteLine("2: Subreddit posts");
				Console.WriteLine("3: exit");

				string choice = "";

				while (choice == "")
				{
					Console.Write("->");
					choice = Console.ReadLine();

					if (choice == "")
						continue;

					switch (choice)
					{
						case "1":
							downloadUserPosts();
							break;
						case "2":
							downloadSubredditPosts();
							break;
						case "3":
							return;
						default:
							choice = "";
							Console.WriteLine("Please enter a correct choice");
							break;
					}
				}

				Console.WriteLine();
				string doRepeat = getUserInput("Do you want to download any other posts? (yes/no)", @"^((?!yes|no).)*$", "Please enter yes or no");
				repeat = doRepeat == "yes";

			} while (repeat);
		}

		private static void downloadUserPosts()
		{
			string username = getUserInput("Please enter the username which you want to download posts from", @"\W", "Please enter a valid username");
			string postCount = getUserInput("How many posts do you want to download? (0 for one page's worth)", @"\D", "Please enter a number");
			string nsfw = getUserInput("Should posts marked NSFW be downloaded (yes/no)", @"^((?!yes|no).)*$", "Please enter yes or no");
			string textPosts = getUserInput("Should text posts be downloaded? (yes/no)", @"^((?!yes|no).)*$", "Please enter yes or no");

			int.TryParse(postCount, out int IPostCount);

			bool BNsfw = nsfw == "yes";
			bool BTextPosts = textPosts == "yes";

			redditPostDownloader.downloadUserPosts(username, IPostCount, BNsfw, BTextPosts);

			if(Directory.Exists(redditPostDownloader.downloadFolder))
				Process.Start(redditPostDownloader.downloadFolder);
		}

		private static void downloadSubredditPosts()
		{
			string subreddit = getUserInput("Please enter the subreddit which you want to download posts from (without the r/)", @"\W", "Please enter a valid username");
			string postCount = getUserInput("How many posts do you want to download? (0 for one page's worth)", @"\D", "Please enter a number");
			string nsfw = getUserInput("Should posts marked NSFW be downloaded (yes/no)", @"^((?!yes|no).)*$", "Please enter yes or no");
			string textPosts = getUserInput("Should text posts be downloaded? (yes/no)", @"^((?!yes|no).)*$", "Please enter yes or no");

			int.TryParse(postCount, out int IPostCount);

			bool BNsfw = nsfw == "yes";
			bool BTextPosts = textPosts == "yes";

			redditPostDownloader.downloadSubredditPosts(subreddit, IPostCount, BNsfw, BTextPosts);

			if (Directory.Exists(redditPostDownloader.downloadFolder))
				Process.Start(redditPostDownloader.downloadFolder);
		}

		private static string getUserInput(string request, string inputRegex, string inputIncorrectMessage)
		{
			Console.WriteLine(request);

			string input = "";

			while (input == "")
			{
				Console.Write("->");
				input = Console.ReadLine();

				if(input == "")
					continue;

				if (Regex.IsMatch(input, inputRegex))
				{
					input = "";
					Console.WriteLine(inputIncorrectMessage);
				}
			}

			return input;
		}
	}
}
