/*
@author Ramadan Ismael
*/

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using server.src.Data;
using server.src.DTOs;
using server.src.Extensions;
using server.src.Models;

namespace server.src.Hubs
{
    [Authorize]
    public class ChatHub(UserManager<AppUser> userManager, AppDbContext context) : Hub
    {
        public static readonly ConcurrentDictionary<string, OnlineUserDto> onlineUsers = new();

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var recevierId = httpContext?.Request.Query["senderId"].ToString();
            var userName = Context.User!.Identity!.Name!;
            var currentUser = await userManager.FindByEmailAsync(userName);
            var connectionId = Context.ConnectionId;


            if(onlineUsers.ContainsKey(userName))
            {
                onlineUsers[userName].ConnectionId = connectionId;
            }
            else
            {
                var user = new OnlineUserDto {
                    ConnectionId = connectionId,
                    UserName = userName,
                    ProfilePicture = currentUser!.ProfileImage,
                    FullName = currentUser!.FullName
                };

                onlineUsers.TryAdd(userName, user);

                await Clients.AllExcept(connectionId).SendAsync("Notify", currentUser);
            }

            if(!string.IsNullOrEmpty(recevierId))
            {
                await LoadMessages(recevierId);
            }

            await Clients.All.SendAsync("OnlineUsers", await GetAllUsers());
        }

        public async Task LoadMessages (string recipientId, int pageNumber = 1)
        {
            int pageSize = 10;
            var username = Context.User!.Identity!.Name;
            var currentUser = await userManager.FindByNameAsync(username!);

            if(currentUser is null)
            {
                return;
            }

            List<MessageResponseDto> messages = await context.Messages
            .Where(x => x.ReceiveId == currentUser!.Id && x.SenderId == recipientId || x.SenderId == currentUser!.Id && x.ReceiveId == recipientId)
            .OrderByDescending(x => x.CreateDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(x => x.CreateDate)
            .Select(x => new MessageResponseDto {
                Id = x.Id,
                Content = x.Content,
                CreateDate = x.CreateDate,
                ReceiveId = x.ReceiveId,
                SenderId = x.SenderId
            })
            .ToListAsync();

            foreach(var message in messages)
            {
                var msg = await context.Messages.FirstOrDefaultAsync(x => x.Id == message.Id);

                if(msg != null && msg.ReceiveId == currentUser.Id)
                {
                    msg.IsRead = true;
                    await context.SaveChangesAsync();
                }
            }

            await Clients.User(currentUser.Id)
            .SendAsync("ReceiveMessageList", messages);
        }

        public async Task SendMessage (MessageRequestDto messageRequestDto)
        {
            var senderId = Context.User!.Identity!.Name;
            var recipientId = messageRequestDto.ReceiveId;

            var newMsg = new Message
            {
                Sender = await userManager.FindByNameAsync(senderId!),
                Receiver = await userManager.FindByIdAsync(recipientId!),
                IsRead = false,
                CreateDate = DateTime.UtcNow,
                Content = messageRequestDto.Content
            };

            //await context.Messages.AddAsync(newMsg);
            context.Messages.Add(newMsg);
            await context.SaveChangesAsync();

            await Clients.User(recipientId!).SendAsync("ReceivedNewMessage", newMsg);
        }

        public async Task NotifyTyping(string recipientUserName)
        {
            var senderUserName = Context.User!.Identity!.Name;

            if(senderUserName is null)
            {
                return;
            }

            var connectionId = onlineUsers.Values.FirstOrDefault(x => x.UserName == recipientUserName)?.ConnectionId;

            if(connectionId != null)
            {
                await Clients.Client(connectionId).SendAsync("NotifyTypingToUser", senderUserName);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User!.Identity!.Name;
            onlineUsers.TryRemove(username!, out _);
            await Clients.All.SendAsync("OnlineUsers", await GetAllUsers());
        }

        private async Task<IEnumerable<OnlineUserDto>> GetAllUsers()
        {
            var username = Context.User!.GetUserName();
            
            var onlineUsersSet = new HashSet<string>(onlineUsers.Keys);

            var users = await userManager.Users.Select(u => new OnlineUserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                ProfilePicture = u.ProfileImage,
                IsOnline = onlineUsersSet.Contains(u.UserName!),
                UnreadCount = context.Messages.Count(x => x.ReceiveId == username && x.SenderId == u.Id && !x.IsRead)
            }).OrderByDescending(u => u.IsOnline).ToListAsync();

            return users;
        }
    }
}