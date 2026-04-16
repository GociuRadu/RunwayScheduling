using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using MediatR;

namespace Api.Validation;

public static class ValidationEndpointFilterExtensions
{
    public static TBuilder AddRequestValidation<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilterFactory((factoryContext, next) =>
        {
            var validationIndexes = factoryContext.MethodInfo
                .GetParameters()
                .Select((parameter, index) => new { parameter, index })
                .Where(item => ShouldValidate(item.parameter.ParameterType))
                .Select(item => item.index)
                .ToArray();

            if (validationIndexes.Length == 0)
            {
                return next;
            }

            return async invocationContext =>
            {
                Dictionary<string, string[]>? errors = null;

                foreach (var index in validationIndexes)
                {
                    var argument = invocationContext.Arguments[index];
                    if (argument is null)
                    {
                        continue;
                    }

                    errors ??= new Dictionary<string, string[]>(StringComparer.Ordinal);
                    ValidateObjectGraph(argument, errors);
                }

                if (errors is { Count: > 0 })
                {
                    return TypedResults.ValidationProblem(errors);
                }

                return await next(invocationContext);
            };
        });

        return builder;
    }

    private static bool ShouldValidate(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (!type.IsClass || type.IsAbstract || type == typeof(string))
        {
            return false;
        }

        if (typeof(IMediator).IsAssignableFrom(type)
            || typeof(HttpContext).IsAssignableFrom(type)
            || typeof(HttpRequest).IsAssignableFrom(type)
            || typeof(HttpResponse).IsAssignableFrom(type)
            || typeof(ClaimsPrincipal).IsAssignableFrom(type))
        {
            return false;
        }

        return true;
    }

    private static void ValidateObjectGraph(object root, IDictionary<string, string[]> errors)
    {
        var queue = new Queue<(object Value, string Prefix)>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var collected = new List<(string Key, string Message)>();

        queue.Enqueue((root, string.Empty));

        while (queue.Count > 0)
        {
            var (current, prefix) = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            var results = new List<ValidationResult>();
            Validator.TryValidateObject(current, new ValidationContext(current), results, validateAllProperties: true);

            foreach (var result in results)
            {
                var members = result.MemberNames.Any()
                    ? result.MemberNames
                    : [string.IsNullOrWhiteSpace(prefix) ? "request" : prefix];

                foreach (var member in members)
                {
                    var key = CombinePath(prefix, member);
                    collected.Add((key, result.ErrorMessage ?? "Invalid value."));
                }
            }

            foreach (var property in current.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead)
                {
                    continue;
                }

                var propertyValue = property.GetValue(current);
                if (propertyValue is null)
                {
                    continue;
                }

                var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var propertyPrefix = CombinePath(prefix, property.Name);

                if (IsSimple(propertyType))
                {
                    continue;
                }

                if (propertyValue is IEnumerable enumerable and not string)
                {
                    var itemIndex = 0;
                    foreach (var item in enumerable)
                    {
                        if (item is null)
                        {
                            itemIndex++;
                            continue;
                        }

                        var itemType = Nullable.GetUnderlyingType(item.GetType()) ?? item.GetType();
                        if (!IsSimple(itemType))
                        {
                            queue.Enqueue((item, $"{propertyPrefix}[{itemIndex}]"));
                        }

                        itemIndex++;
                    }

                    continue;
                }

                queue.Enqueue((propertyValue, propertyPrefix));
            }
        }

        foreach (var group in collected.GroupBy(item => item.Key, StringComparer.Ordinal))
        {
            errors[group.Key] = group
                .Select(item => item.Message)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }

    private static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(Guid)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(decimal);
    }

    private static string CombinePath(string prefix, string member)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return member;
        }

        if (string.IsNullOrWhiteSpace(member) || member == "request")
        {
            return prefix;
        }

        return member.StartsWith("[", StringComparison.Ordinal)
            ? $"{prefix}{member}"
            : $"{prefix}.{member}";
    }
}
