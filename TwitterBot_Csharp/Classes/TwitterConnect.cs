﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Drawing.Imaging;
using TweetSharp;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data;

namespace TwitterBot_Csharp.Classes
{
    public static class PoetryBot
    {
        public static Stream ToStream(this Image image, ImageFormat format)
        {
            var stream = new System.IO.MemoryStream();
            image.Save(stream, format);
            stream.Position = 0;
            return stream;
        }

        public static HtmlAgilityPack.HtmlDocument LinkToHtmlDoc(string url, bool utf8 = false) // default to non-utf8 unless made explicit
        {
            WebClient client = new WebClient();
            if (utf8 == true)
            {
                client.Encoding = Encoding.UTF8;
            }// to deal w/ odd chars
            string htmlString = client.DownloadString(url);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlString);
            return doc;
        }

        public static Image PublicDomainPoetryRndmToImage()
        {

            //get random page
            Random rnd = new Random();
            int page = rnd.Next(1, 500);
            string url = "http://www.public-domain-poetry.com/listpoetry.php?letter=All&page=" + page.ToString();

            var doc = PoetryBot.LinkToHtmlDoc(url);

            HtmlNodeCollection poemLinks = doc.DocumentNode.SelectNodes("//a"); // get all links
            List<string> strPoems = new List<string>();

            foreach (var link in poemLinks)
            {
                var href = link.Attributes["href"].Value;
                if (href.ToString().Contains("php") == false
                    && href.ToString().Contains("http") == false
                    && href.ToString().Substring(href.Length - 1) != @"/"
                    && href.ToString().Any(char.IsDigit))
                {
                    strPoems.Add(href); // get just hrefs
                }
            }

            //get random poem
            int rndPoem = rnd.Next(1, strPoems.Count);
            url = "http://www.public-domain-poetry.com/" + strPoems[rndPoem];
            doc = PoetryBot.LinkToHtmlDoc(url);

            HtmlNode title = doc.DocumentNode.SelectSingleNode("//font[@class='t0']");
            HtmlNode poem = doc.DocumentNode.SelectSingleNode("//font[@class='t3a']");

            string justTitle = title.InnerText.Substring(0, title.InnerText.LastIndexOf(" by "));
            string justAuthor = title.InnerText.Substring(title.InnerText.LastIndexOf(" by ")+1);

            string htmlConcat = @"<div style=""text-indent: -1em; padding-left: 1em;""> <b>" +
                            justTitle.Replace("Public Domain Poetry - ", "").ToUpper() + @"</b> " + 
                            justAuthor.ToUpper() + @"<br><br>" + 
                            poem.OuterHtml + @"</div>";

            Image image = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.RenderToImage(htmlConcat);

            return image;

        }

        public static Image PoetryFoundationRndmToImage()
        {

            Random rnd = new Random();
            int poemInt = rnd.Next(50100, 58800);

            string url = "https://www.poetryfoundation.org/poems-and-poets/poems/detail/" + poemInt.ToString(); //90621


            var doc = LinkToHtmlDoc(url, true);

            HtmlNode poemDiv = doc.DocumentNode.SelectSingleNode("//div[@class='poem']");
            HtmlNode titleSpan = doc.DocumentNode.SelectSingleNode("//span[@class='hdg hdg_1']");
            HtmlNode authSpan = doc.DocumentNode.SelectSingleNode("//span[@class='hdg hdg_utility']");

            string htmlConcat =

                    @"<div style=""text-indent: -1em; padding-left: 1em;""> <b>" + titleSpan.InnerText.ToString().ToUpper() + @"</b>"
                + authSpan.InnerText.ToString().ToUpper() + @"</div><br>"
                + poemDiv.OuterHtml.ToString();


            Image image = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.RenderToImage(htmlConcat);

            return image;

        }


        public static Image PoetsorgToImage()
        {

            //get random page
            Random rnd = new Random();
            int rndPage = rnd.Next(1, 698);
            string url = "https://www.poets.org/poetsorg/poems?field_occasion_tid=All&field_poem_themes_tid=All&field_form_tid=All&page=" + rndPage.ToString();
            var doc = LinkToHtmlDoc(url, true);

            HtmlNodeCollection poemLinks = doc.DocumentNode.SelectNodes("//a"); // get all links
            List<string> strPoems = new List<string>();
            foreach (var link in poemLinks)
            {

                if (link.OuterHtml.Contains("href"))
                {
                    var href = link.Attributes["href"].Value;
                    if (href.ToString().Contains(@"poetsorg/poem/"))
                    {
                        strPoems.Add(href); // get just hrefs
                    }

                }
            }

            //get random poem
            int rndPoem = rnd.Next(1, strPoems.Count);
            url = "https://www.poets.org" + strPoems[rndPoem]; // base link needed
            doc = PoetryBot.LinkToHtmlDoc(url, true);

            HtmlNode poemDiv = doc.DocumentNode.SelectSingleNode("//div[@id='poem-content'] //div[@class='field-item even']");
            HtmlNode authSpan = doc.DocumentNode.SelectSingleNode("//div[@id='poem-content'] //span[@class='node-title']");
            HtmlNode titleSpan = doc.DocumentNode.SelectSingleNode("//div[@id='poem-content'] //h1[@id='page-title']");
            string htmlConcat =
                @"<div style=""text-indent: -1em; padding-left: 1em;""> <b>" + titleSpan.InnerText.ToString().ToUpper() + @"</b>"
            + " BY " + authSpan.InnerText.ToString().ToUpper() + @"<br>"
            + poemDiv.OuterHtml.ToString() + @"</div>";
                            
            Image image = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.RenderToImage(htmlConcat);

            //Image image = null;
            return image;

        }


        public static void PostToBot(string post, Stream stream)
        {
            /* TweetSharp is no longer being updated but is necessary. TweetMoaSharp is the current
            updated package. Use Update-Package TweetMoaSharp to update it. Use Update-Package -reinstall TweetMoaSharp
            to reinstall the entire package. It must be installed ON TOP of existing final TweetSharp package.
             */

            string _consumerKey = ConfigurationManager.AppSettings["consumerKey"];
            string _consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
            string _accessToken = ConfigurationManager.AppSettings["accessToken"];
            string _accessTokenSecret = ConfigurationManager.AppSettings["accessTokenSecret"];


            var service = new TwitterService(_consumerKey, _consumerSecret);
            service.AuthenticateWith(_accessToken, _accessTokenSecret);
            
            service.SendTweetWithMedia(new SendTweetWithMediaOptions
            {
//                Status = post,
                Images = new Dictionary<string, Stream> { { "john", stream } }
            });
               
//            service.SendTweet(new SendTweetOptions { Status = post }); 
        }

        public static void FollowPoetryHashtaggers(int cnt)
        {

            string _consumerKey = ConfigurationManager.AppSettings["consumerKey"];
            string _consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
            string _accessToken = ConfigurationManager.AppSettings["accessToken"];
            string _accessTokenSecret = ConfigurationManager.AppSettings["accessTokenSecret"];

            var service = new TwitterService(_consumerKey, _consumerSecret);
            service.AuthenticateWith(_accessToken, _accessTokenSecret);

            var tweets = service.Search(new SearchOptions { Q = "#poetry", Count = cnt, Resulttype = TwitterSearchResultType.Popular, IncludeEntities = false });

            foreach (var tweet in tweets.Statuses)
            {
//                Console.WriteLine(tweet.User.ScreenName);
                service.FollowUser(new FollowUserOptions { UserId = tweet.User.Id } );
            }

        }

        public static void FollowBackNotFollowed()
        {
            string _consumerKey = ConfigurationManager.AppSettings["consumerKey"];
            string _consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
            string _accessToken = ConfigurationManager.AppSettings["accessToken"];
            string _accessTokenSecret = ConfigurationManager.AppSettings["accessTokenSecret"];

            var service = new TwitterService(_consumerKey, _consumerSecret);
            service.AuthenticateWith(_accessToken, _accessTokenSecret);

            TwitterUser self = service.GetUserProfile(new GetUserProfileOptions() { IncludeEntities = false, SkipStatus = false });

            ListFollowersOptions options = new ListFollowersOptions();
            options.UserId = self.Id;
            options.IncludeUserEntities = true;
            TwitterCursorList<TwitterUser> followers = service.ListFollowers(options);

            foreach (var follow in followers)
            {
               service.FollowUser(new FollowUserOptions { UserId = follow.Id }); //seems to work w/ no errors, even if already followed
            }

        }


        public static void LogTweetInfo()
        {
//            SqlConnection conn = new SqlConnection("Data Source=KIRKBOZEMAN98C1\\SQLEXPRESS;Initial Catalog=Sandbox;Integrated Security=SSPI;");

            var connectionString = ConfigurationManager.ConnectionStrings["LogConnect"].ConnectionString;
            
            string query = @"INSERT INTO dbo.PoetryBotLog(Link, RandomInt, PostDatetime) "
                            + @"VALUES(@link, @randomint, @postdatetime)";

            using (var conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {

                    cmd.Parameters.Add("@link", SqlDbType.VarChar);
                    cmd.Parameters.Add("@randomint", SqlDbType.Int);
                    cmd.Parameters.Add("@postdatetime", SqlDbType.VarChar);


                    cmd.Parameters["@link"].Value = "http://link"; // can use array location or param name
                    cmd.Parameters["@randomint"].Value = 3;
                    cmd.Parameters["@postdatetime"].Value = DateTime.Now;

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
            



        }


    }
}
