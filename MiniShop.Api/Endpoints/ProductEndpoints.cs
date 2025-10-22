using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MiniShop.Api.Data;
using MiniShop.Api.Models;

namespace MiniShop.Api;

public static class ProductEndpoints
{
    // v1: full entity + paging
    public static RouteGroupBuilder MapProductEndpoints(this RouteGroupBuilder g)
    {
        g.MapGet("/products", async Task<Ok<Paged<Product>>> (
            int page = 1, int pageSize = 20, string? search = null, AppDbContext db = null!) =>
        {
            IQueryable<Product> q = db.Products.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(p => p.Name.Contains(search));

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(p => p.CreatedUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return TypedResults.Ok(new Paged<Product>(items, page, pageSize, total));
        });

        g.MapPost("/products", async Task<Results<Created<Product>, ValidationProblem>> (Product p, AppDbContext db) =>
        {
            // validation handled by FluentValidation; invalid -> ProblemDetails
            await using var tx = await db.Database.BeginTransactionAsync();
            db.Products.Add(p);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return TypedResults.Created($"/products/{p.Id}", p);
        });

        g.MapGet("/products/{id:int}", async Task<Results<Ok<Product>, NotFound>> (int id, AppDbContext db) =>
        {
            var prod = await db.Products.FindAsync(id);
            return prod is null ? TypedResults.NotFound() : TypedResults.Ok(prod);
        });

        g.MapDelete("/products/{id:int}", async Task<Results<NoContent, NotFound>> (int id, AppDbContext db) =>
        {
            var prod = await db.Products.FindAsync(id);
            if (prod is null) return TypedResults.NotFound();
            db.Remove(prod);
            await db.SaveChangesAsync();
            return TypedResults.NoContent();
        });

        return g;
    }

    // v2: different shape (adds Currency), same route but under /v2
    public static RouteGroupBuilder MapProductEndpointsV2(this RouteGroupBuilder g)
    {
        g.MapGet("/products", async (AppDbContext db) =>
        {
            var items = await db.Products.AsNoTracking()
                .Select(p => new ProductV2Dto(p.Id, p.Sku, p.Name, p.Price, "GBP"))
                .ToListAsync();

            return TypedResults.Ok(items); // Ok<List<ProductV2Dto>>
        });

        return g;
    }
}

public record Paged<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
