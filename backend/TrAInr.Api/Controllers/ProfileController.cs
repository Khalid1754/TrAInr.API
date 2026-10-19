using Microsoft.AspNetCore.Mvc;
using TrAInr.Api.Data;
using TrAInr.Api.Dtos;
using TrAInr.Api.Models;

namespace TrAInr.Api.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(TrainrDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<UserProfile> Get() => await db.Profiles.FindAsync(1) ?? new UserProfile { Id = 1 };

    [HttpPut]
    public async Task<UserProfile> Update(ProfileInput input)
    {
        var profile = await db.Profiles.FindAsync(1);
        if (profile is null) db.Profiles.Add(profile = new UserProfile { Id = 1 });
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
