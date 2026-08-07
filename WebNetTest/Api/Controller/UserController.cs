public static class UserController
{
    public static void  MapUserController(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users/register", CreateUser);
    }
    private static IResult CreateUser()
    {
        return Results.Created();
    }
}