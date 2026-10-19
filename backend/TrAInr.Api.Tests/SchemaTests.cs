using System.Text.Json;
using TrAInr.Api.Services;

namespace TrAInr.Api.Tests;

public class SchemaTests
{
    [Fact]
    public void Schema_RequiresAllFieldsAndForbidsExtras()
    {
        var schema = JsonSerializer.SerializeToElement(TrainerAi.SchemaFor<MealResult>());
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("ingredients", required);
        Assert.Contains("macros", required);
        var macros = schema.GetProperty("properties").GetProperty("macros");
        Assert.False(macros.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal("Varför måltiden passar just det här passet", schema.GetProperty("properties").GetProperty("why").GetProperty("description").GetString());
    }
}
