using System.Security.Claims;
using Application.Dto_s.Message;
using Application.Interfaces.Services.MainServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HabitHub.Controllers;

[Authorize]
public class ChatHub(IMessageService messageService) : Hub
{
    public async Task SendMessage(Guid recipientId, string text)
    {
        try
        {
            var senderId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            
            var addResult = await messageService.AddAsync(new MessageAddDto
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Text = text
            });

            if (!addResult.IsSuccess)
            {
                await Clients.Caller.SendAsync("Error", addResult.Error!.Message);
                return;
            }
        
            var messageResult = await messageService.GetByIdAsync(senderId, addResult.Value!.Id);
            if (!messageResult.IsSuccess)
            {
                await Clients.Caller.SendAsync("Error", "Failed to retrieve sent message");
                return;
            }

            var message = messageResult.Value!;
        
            await Task.WhenAll(
                Clients.User(senderId.ToString()).SendAsync("ReceiveMessage", message),
                Clients.User(recipientId.ToString()).SendAsync("ReceiveMessage", message)
            );
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send message: {ex.Message}");
        }
    }

    public async Task EditMessage(Guid messageId, string newText)
    {
        try
        {
            var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            
            var updateResult = await messageService.UpdateAsync(userId, new MessagePutDto
            {
                Id = messageId,
                Text = newText
            });

            if (!updateResult.IsSuccess)
            {
                await Clients.Caller.SendAsync("Error", updateResult.Error!.Message);
                return;
            }

            var messageResult = await messageService.GetByIdAsync(userId, messageId);
            if (!messageResult.IsSuccess)
            {
                await Clients.Caller.SendAsync("Error", "Failed to retrieve updated message");
                return;
            }

            var updatedMessage = messageResult.Value!;
            var companionId = updatedMessage.SenderId == userId 
                ? updatedMessage.RecipientId 
                : updatedMessage.SenderId;

            await Task.WhenAll(
                Clients.User(userId.ToString()).SendAsync("UpdateMessage", updatedMessage),
                Clients.User(companionId.ToString()).SendAsync("UpdateMessage", updatedMessage)
            );
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to edit message: {ex.Message}");
        }
    }

    public async Task DeleteMessage(Guid messageId)
    {
        try
        {
            var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            
            var messageResult = await messageService.GetByIdAsync(userId, messageId);
            if (!messageResult.IsSuccess)
            {
                await Clients.Caller.SendAsync("Error", messageResult.Error!.Message);
                return;
            }

            var message = messageResult.Value!;
            var companionId = message.SenderId == userId 
                ? message.RecipientId 
                : message.SenderId;

            var deleteResult = await messageService.DeleteAsync(userId, messageId);
            if (!deleteResult.IsSuccess)
            {
                await Clients.Caller.SendAsync("Error", deleteResult.Error!.Message);
                return;
            }

            await Task.WhenAll(
                Clients.User(userId.ToString()).SendAsync("DeleteMessage", messageId),
                Clients.User(companionId.ToString()).SendAsync("DeleteMessage", messageId)
            );
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to delete message: {ex.Message}");
        }
    }

    public async Task GetChatHistory(Guid companionId)
    {
        try
        {
            var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            
            var result = await messageService.GetAllByUsersIdAsync(userId, companionId);
            
            if (!result.IsSuccess)
            {
                await Clients.Caller.SendAsync("Error", result.Error!.Message);
                return;
            }

            await Clients.Caller.SendAsync("ChatHistory", companionId, result.Value);
        }
        catch (Exception)
        {
            await Clients.Caller.SendAsync("Error", "Failed to load chat history");
        }
    }

    public async Task GetAllCompanions()
    {
        try
        {
            var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            
            var result = await messageService.GetAllCompanionsIdByUserIdAsync(userId);
            
            if (!result.IsSuccess)
            {
                await Clients.Caller.SendAsync("Error", result.Error!.Message);
                return;
            }

            await Clients.Caller.SendAsync("CompanionsList", result.Value!.Select(x => x.Id));
        }
        catch (Exception)
        {
            await Clients.Caller.SendAsync("Error", "Failed to load companions");
        }
    }
}