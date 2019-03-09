using System;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace FluentValidationExtensions
{
	/// <summary>
	/// Extension methods for working with a Service Provider.
	/// </summary>
	public static class DependencyInjectionExtensions
	{

		/// <summary>
		/// Gets the service provider associated with the validation context.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public static IServiceProvider GetServiceProvider(this IValidationContext context)
		{
			ValidationContext actualContext = null;

			switch (context)
			{
				case PropertyValidatorContext pvc:
					actualContext = pvc.ParentContext;
					break;
				case ValidationContext vc:
					actualContext = vc;
					break;
			}

			if (actualContext != null)
			{
				if (actualContext.RootContextData.TryGetValue("_FV_ServiceProvider", out var sp))
				{
					if (sp is IServiceProvider serviceProvider)
					{
						return serviceProvider;
					}
				}

			}

			throw new InvalidOperationException(
				"The service provider has not been configured to work with FluentValidation. Making use of InjectValidator or GetServiceProvider is only supported when using the automatic MVC integration.");
		}

		/// <summary>
		/// Sets the service provider associated with the validation context.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="serviceProvider"></param>
		public static void SetServiceProvider(this ValidationContext context, IServiceProvider serviceProvider)
		{
			context.RootContextData["_FV_ServiceProvider"] = serviceProvider;
		}

		/// <summary>
		/// Uses the Service Provider to inject the default validator for the property type.
		/// </summary>
		/// <param name="ruleBuilder"></param>
		/// <param name="ruleSets"></param>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty> InjectValidator<T, TProperty>(
			this IRuleBuilder<T, TProperty> ruleBuilder, params string[] ruleSets)
		{
			return ruleBuilder.InjectValidator((s, ctx) => s.GetService<IValidatorFactory>().GetValidator<TProperty>(),
				ruleSets);
		}

		/// <summary>
		/// Uses the Service Provider to inject the default validator for the property type.
		/// </summary>
		/// <param name="ruleBuilder"></param>
		/// <param name="callback"></param>
		/// <param name="ruleSets"></param>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty> InjectValidator<T, TProperty>(
			this IRuleBuilder<T, TProperty> ruleBuilder,
			Func<IServiceProvider, ValidationContext<T>, IValidator<TProperty>> callback, params string[] ruleSets)
		{
			var adaptor = new ChildValidatorAdaptor(context =>
			{
				var actualContext = (PropertyValidatorContext) context;
				var serviceProvider = actualContext.ParentContext.GetServiceProvider();
				var validator = callback(serviceProvider, (ValidationContext<T>) actualContext.ParentContext);
				return validator;
			}, typeof(IValidator<TProperty>));

			adaptor.RuleSets = ruleSets;

			return ruleBuilder.SetValidator(adaptor);
		}
	}
}