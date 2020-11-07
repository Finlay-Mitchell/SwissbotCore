﻿using SwissbotCore.Handlers;
using SwissbotCore.http;
using SwissbotCore.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SwissbotCore.HTTP.Routes
{
    public class Modlogs
    {
        [Route(@"(^\/modlogs$)|(^\/modlogs\?q=(.*?)$)|\/modlogs\?f=(.*?)&t=(.*?)$", "GET", true)]
        public static async Task getModlogs(HttpListenerContext c, MatchCollection m)
        {
            try
            {
               
                // Check if they have the discord auth
                if (!c.Request.Cookies.Any(x => x.Name == "csSessionID"))
                {
                    c.Response.Redirect($"https://discord.com/api/oauth2/authorize?client_id=772314985979969596&redirect_uri=https%3A%2F%2Fapi.swissdev.team%2Fapprentice%2Fv1%2Fauth&response_type=code&scope=identify&state={UrlEncoder.Default.Encode(c.Request.RawUrl)}");
                    c.Response.Close();

                    return;
                }

                var sesh = c.Request.Cookies["csSessionID"];

                if (!DiscordAuthKeeper.IsValidUser(sesh))
                {
                    c.Response.StatusCode = 401;
                    c.Response.Close();
                    return;
                }

                var user = DiscordAuthKeeper.GetUser(sesh.Value);

                if (!user.HasPermissions())
                {
                    c.Response.StatusCode = 403;
                    c.Response.Close();

                    return;
                }

                if (c.Request.QueryString.Count == 0)
                {
                    var requestingUser = Global.Client.GetUser(user.ID);
                    var pfp = requestingUser.GetAvatarUrl(Discord.ImageFormat.Jpeg);
                    if (pfp == null)
                        pfp = requestingUser.GetDefaultAvatarUrl();

                    string modlogs = "";
                    // Generate the modlogs
                    foreach (var item in ModDatabase.currentLogs.Users.Where(x => x.Logs.Count > 0).Take(25))
                    {
                        string logs = "";

                        foreach (var log in item.Logs.OrderBy(x => DateTime.Parse(x.Date).Ticks).Reverse())
                        {
                            var mod = "Unknown Moderator";
                            var modUser = Global.Client.GetUser(log.ModeratorID);
                            if (modUser != null)
                                mod = modUser.ToString();

                            logs += Properties.Resources.modlogItem
                                .Replace("{modlog.id}", log.InfractionID)
                                .Replace("{modlog.action}", log.Action.ToString())
                                .Replace("{modlog.date}", log.Date)
                                .Replace("{modlog.reason}", log.Reason)
                                .Replace("{modlog.moderator}", mod);
                        }

                        var modlogUser = Global.Client.GetUser(item.userId);

                        string modlogpfp = "https://encrypted-tbn0.gstatic.com/images?q=tbn%3AANd9GcTjA0Lpsg840JNGLaPgVWM9QofkvAYdFPLb-g&usqp=CAU";
                        if (modlogUser != null)
                        {
                            modlogpfp = modlogUser.GetAvatarUrl(Discord.ImageFormat.Jpeg);
                            if (modlogpfp == null)
                            {
                                modlogpfp = modlogUser.GetDefaultAvatarUrl();
                            }
                        }
                        string username = item.username;

                        if (modlogUser != null)
                            username = modlogUser.ToString();

                        modlogs += Properties.Resources.modlogUser
                            .Replace("{user.profile}", modlogpfp)
                            .Replace("{user.id}", item.userId.ToString())
                            .Replace("{user.username}", username)
                            .Replace("{user.mostRecent}", item.Logs.OrderBy(x => DateTime.Parse(x.Date).Ticks).Last().Date)
                            .Replace("{user.modlogs}", logs);
                    }

                    // Generate the HTML
                    string html = Properties.Resources.index
                        .Replace("{user.profile}", pfp)
                        .Replace("{user.username}", requestingUser.ToString())
                        .Replace("{modlogs}", modlogs);

                    // Return the html
                    c.Response.OutputStream.Write(Encoding.UTF8.GetBytes(html));
                    c.Response.ContentEncoding = Encoding.UTF8;
                    c.Response.ContentType = "text/html";
                    c.Response.StatusCode = 200;
                }
                else
                {
                    if (c.Request.QueryString.AllKeys.Contains("q"))
                    {
                        // They want a query!
                        var query = c.Request.QueryString["q"];

                        if (query == null)
                        {
                            c.Response.StatusCode = 400;
                            c.Response.Close();
                            return;
                        }

                        // Query the modlogs
                        var users = ModDatabase.currentLogs.Users.Where(x => x.userId.ToString().StartsWith(query) || x.username.StartsWith(query)).Take(25);

                        string modlogs = "";

                        foreach (var item in users)
                        {
                            string logs = "";

                            foreach (var log in item.Logs.OrderBy(x => DateTime.Parse(x.Date).Ticks).Reverse())
                            {
                                var mod = "Unknown Moderator";
                                var modUser = Global.Client.GetUser(log.ModeratorID);
                                if (modUser != null)
                                    mod = modUser.ToString();

                                logs += Properties.Resources.modlogItem
                                    .Replace("{modlog.id}", log.InfractionID)
                                    .Replace("{modlog.action}", log.Action.ToString())
                                    .Replace("{modlog.date}", log.Date)
                                    .Replace("{modlog.reason}", log.Reason)
                                    .Replace("{modlog.moderator}", mod);
                            }

                            var modlogUser = Global.Client.GetUser(item.userId);

                            string modlogpfp = "https://encrypted-tbn0.gstatic.com/images?q=tbn%3AANd9GcTjA0Lpsg840JNGLaPgVWM9QofkvAYdFPLb-g&usqp=CAU";
                            if (modlogUser != null)
                            {
                                modlogpfp = modlogUser.GetAvatarUrl(Discord.ImageFormat.Jpeg);
                                if (modlogpfp == null)
                                {
                                    modlogpfp = modlogUser.GetDefaultAvatarUrl();
                                }
                            }
                            string username = item.username;

                            if (modlogUser != null)
                                username = modlogUser.ToString();

                            modlogs += Properties.Resources.modlogUser
                                .Replace("{user.profile}", modlogpfp)
                                .Replace("{user.id}", item.userId.ToString())
                                .Replace("{user.username}", username)
                                .Replace("{user.mostRecent}", item.Logs.OrderBy(x => DateTime.Parse(x.Date).Ticks).Last().Date)
                                .Replace("{user.modlogs}", logs);
                        }

                        c.Response.OutputStream.Write(Encoding.UTF8.GetBytes(modlogs));
                        c.Response.ContentEncoding = Encoding.UTF8;
                        c.Response.StatusCode = 200;
                        c.Response.Close();
                    }
                    else if(c.Request.QueryString.AllKeys.Contains("f") && c.Request.QueryString.AllKeys.Contains("t"))
                    {
                        // From and to time!
                        // Parse the url params to ints
                        var from = c.Request.QueryString["f"];
                        int fromVal = 0;

                        var to = c.Request.QueryString["t"];
                        int toVal = 0;

                        if(!int.TryParse(from, out fromVal))
                        {
                            c.Response.StatusCode = 400;
                            c.Response.Close();
                            return;
                        }

                        if(!int.TryParse(to, out toVal))
                        {
                            c.Response.StatusCode = 400;
                            c.Response.Close();
                            return;
                        }

                        if(fromVal > toVal)
                        {
                            c.Response.StatusCode = 400;
                            c.Response.Close();
                            return;
                        }

                        if(toVal - fromVal > 30)
                        {
                            c.Response.StatusCode = 400;
                            c.Response.Close();
                            return;
                        }

                        var users = ModDatabase.currentLogs.Users.Skip(fromVal).Take(toVal);

                        string modlogs = "";

                        foreach (var item in users)
                        {
                            string logs = "";

                            foreach (var log in item.Logs.OrderBy(x => DateTime.Parse(x.Date).Ticks).Reverse())
                            {
                                var mod = "Unknown Moderator";
                                var modUser = Global.Client.GetUser(log.ModeratorID);
                                if (modUser != null)
                                    mod = modUser.ToString();

                                logs += Properties.Resources.modlogItem
                                    .Replace("{modlog.id}", log.InfractionID)
                                    .Replace("{modlog.action}", log.Action.ToString())
                                    .Replace("{modlog.date}", log.Date)
                                    .Replace("{modlog.reason}", log.Reason)
                                    .Replace("{modlog.moderator}", mod);
                            }

                            var modlogUser = Global.Client.GetUser(item.userId);

                            string modlogpfp = "https://encrypted-tbn0.gstatic.com/images?q=tbn%3AANd9GcTjA0Lpsg840JNGLaPgVWM9QofkvAYdFPLb-g&usqp=CAU";
                            if (modlogUser != null)
                            {
                                modlogpfp = modlogUser.GetAvatarUrl(Discord.ImageFormat.Jpeg);
                                if (modlogpfp == null)
                                {
                                    modlogpfp = modlogUser.GetDefaultAvatarUrl();
                                }
                            }
                            string username = item.username;

                            if (modlogUser != null)
                                username = modlogUser.ToString();

                            modlogs += Properties.Resources.modlogUser
                                .Replace("{user.profile}", modlogpfp)
                                .Replace("{user.id}", item.userId.ToString())
                                .Replace("{user.username}", username)
                                .Replace("{user.mostRecent}", item.Logs.OrderBy(x => DateTime.Parse(x.Date).Ticks).Last().Date)
                                .Replace("{user.modlogs}", logs);
                        }

                        c.Response.OutputStream.Write(Encoding.UTF8.GetBytes(modlogs));
                        c.Response.ContentEncoding = Encoding.UTF8;
                        c.Response.StatusCode = 200;
                        c.Response.Close();
                    }
                }
               
            }
            catch(Exception x)
            {
                c.Response.StatusCode = 500;
                c.Response.Close();
            }
        }
    }
}
