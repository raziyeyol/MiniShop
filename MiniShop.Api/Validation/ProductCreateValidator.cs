using FluentValidation;
using MiniShop.Api.Models;

namespace MiniShop.Api.Validation;
public class ProductCreateValidator : AbstractValidator<Product>
{
    public ProductCreateValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}