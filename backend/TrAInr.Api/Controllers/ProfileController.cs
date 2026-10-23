using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrAInr.Api.Data;
using TrAInr.Api.Dtos;
using TrAInr.Api.Models;
using TrAInr.Api.Services;

namespace TrAInr.Api.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(TrainrDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<UserProfile> Get()
    {
        var userId = User.UserId();
        return await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId) ?? new UserProfile { UserId = userId };
    }

    [HttpPut]
    public async Task<UserProfile> Update(ProfileInput input)
    {
        var userId = User.UserId();
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is null) db.Profiles.Add(profile = new UserProfile { UserId = userId });
        profile.Name = input.Name;
        profile.BodyWeightKg = input.BodyWeightKg;
        profile.HeightCm = input.HeightCm;
        profile.Age = input.Age;
        profile.Sex = input.Sex;
        profile.Goal = input.Goal;
        profile.Experience = input.Experience;
        profile.DaysPerWeek = input.DaysPerWeek;
        profile.DietaryPreferences = input.DietaryPreferences;
        await db.SaveChangesAsync();
        return profile;
    }
}
