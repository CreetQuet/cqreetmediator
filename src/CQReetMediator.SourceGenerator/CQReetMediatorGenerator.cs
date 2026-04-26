using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CQReetMediator.SourceGenerator
{
    [Generator]
    public class CQReetMediatorGenerator : IIncrementalGenerator
    {
        private const string RequestHandler2 = "CQReetMediator.Abstractions.IRequestHandler`2";
        private const string RequestHandler1 = "CQReetMediator.Abstractions.IRequestHandler`1";
        private const string NotificationHandler1 = "CQReetMediator.Abstractions.INotificationHandler`1";
        private const string PipelineBehavior2 = "CQReetMediator.Abstractions.IPipelineBehavior`2";
        private const string PipelineBehavior1 = "CQReetMediator.Abstractions.IPipelineBehavior`1";

        private static readonly SymbolDisplayFormat FullyQualified = SymbolDisplayFormat.FullyQualifiedFormat;

        private static readonly SymbolDisplayFormat NameOnly = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.None);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var registrations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsCandidate(node),
                    transform: static (ctx, ct) => Extract(ctx, ct))
                .Where(static x => x.Length > 0)
                .SelectMany(static (x, _) => x);

            var collected = registrations.Collect();

            context.RegisterSourceOutput(collected, static (spc, regs) =>
            {
                var source = Emit(regs);
                spc.AddSource("CQReetMediatorDependencyInjection.g.cs",
                    Microsoft.CodeAnalysis.Text.SourceText.From(source, Encoding.UTF8));
            });
        }

        private static bool IsCandidate(SyntaxNode node)
        {
            if (!(node is ClassDeclarationSyntax cds))
                return false;
            if (cds.BaseList == null || cds.BaseList.Types.Count == 0)
                return false;
            if (cds.Modifiers.Any(SyntaxKind.AbstractKeyword) || cds.Modifiers.Any(SyntaxKind.StaticKeyword))
                return false;
            return true;
        }

        private static ImmutableArray<RegistrationInfo> Extract(GeneratorSyntaxContext ctx, CancellationToken ct)
        {
            var classDecl = (ClassDeclarationSyntax)ctx.Node;
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl, ct) as INamedTypeSymbol;

            if (symbol == null || symbol.IsAbstract || symbol.IsStatic)
                return ImmutableArray<RegistrationInfo>.Empty;

            var builder = ImmutableArray.CreateBuilder<RegistrationInfo>();

            foreach (var iface in symbol.AllInterfaces)
            {
                if (!iface.IsGenericType)
                    continue;

                var meta = GetMetadataName(iface.OriginalDefinition);

                if (meta == RequestHandler2)
                {
                    var reqType = iface.TypeArguments[0].ToDisplayString(FullyQualified);
                    var respType = iface.TypeArguments[1].ToDisplayString(FullyQualified);
                    var handler = symbol.ToDisplayString(FullyQualified);
                    builder.Add(new RegistrationInfo("RH", handler, reqType, respType, false));
                }
                else if (meta == RequestHandler1)
                {
                    var reqType = iface.TypeArguments[0].ToDisplayString(FullyQualified);
                    var handler = symbol.ToDisplayString(FullyQualified);
                    builder.Add(new RegistrationInfo("VRH", handler, reqType, "", false));
                }
                else if (meta == NotificationHandler1)
                {
                    var notifType = iface.TypeArguments[0].ToDisplayString(FullyQualified);
                    var handler = symbol.ToDisplayString(FullyQualified);
                    builder.Add(new RegistrationInfo("NH", handler, notifType, "", false));
                }
                else if (meta == PipelineBehavior2)
                {
                    bool isOpen = iface.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);
                    if (isOpen)
                    {
                        var handler = OpenGenericTypeof(symbol.OriginalDefinition);
                        builder.Add(new RegistrationInfo("PB", handler, "", "", true));
                    }
                    else
                    {
                        var handler = symbol.ToDisplayString(FullyQualified);
                        var reqType = iface.TypeArguments[0].ToDisplayString(FullyQualified);
                        var respType = iface.TypeArguments[1].ToDisplayString(FullyQualified);
                        builder.Add(new RegistrationInfo("PB", handler, reqType, respType, false));
                    }
                }
                else if (meta == PipelineBehavior1)
                {
                    bool isOpen = iface.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);
                    if (isOpen)
                    {
                        var handler = OpenGenericTypeof(symbol.OriginalDefinition);
                        builder.Add(new RegistrationInfo("VPB", handler, "", "", true));
                    }
                    else
                    {
                        var handler = symbol.ToDisplayString(FullyQualified);
                        var reqType = iface.TypeArguments[0].ToDisplayString(FullyQualified);
                        builder.Add(new RegistrationInfo("VPB", handler, reqType, "", false));
                    }
                }
            }

            return builder.Count > 0 ? builder.ToImmutable() : ImmutableArray<RegistrationInfo>.Empty;
        }

        private static string GetMetadataName(INamedTypeSymbol symbol)
        {
            var ns = symbol.ContainingNamespace;
            if (ns == null || ns.IsGlobalNamespace)
                return symbol.MetadataName;
            return ns.ToDisplayString() + "." + symbol.MetadataName;
        }

        private static string OpenGenericTypeof(INamedTypeSymbol symbol)
        {
            var baseName = symbol.ToDisplayString(NameOnly);
            var commas = symbol.Arity > 1 ? new string(',', symbol.Arity - 1) : "";
            return baseName + "<" + commas + ">";
        }

        private static string Emit(ImmutableArray<RegistrationInfo> registrations)
        {
            var distinct = registrations.Distinct().ToList();

            var sb = new StringBuilder(4096);

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#pragma warning disable");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("namespace CQReetMediator.DependencyInjection");
            sb.AppendLine("{");
            sb.AppendLine(
                "    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"CQReetMediator.SourceGenerator\", \"1.0.0\")]");
            sb.AppendLine("    public static class CQReetMediatorRegistration");
            sb.AppendLine("    {");
            sb.AppendLine(
                "        internal static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddCQReetMediator(");
            sb.AppendLine(
                "            this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
            sb.AppendLine("        {");

            // --- Handler registrations ---
            foreach (var reg in distinct)
            {
                switch (reg.Kind)
                {
                    case "RH":
                        sb.AppendLine(string.Format(
                            "            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddTransient<global::CQReetMediator.Abstractions.IRequestHandler<{0}, {1}>, {2}>(services);",
                            reg.RequestType, reg.ResponseType, reg.HandlerType));
                        break;
                    case "VRH":
                        sb.AppendLine(string.Format(
                            "            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddTransient<global::CQReetMediator.Abstractions.IRequestHandler<{0}>, {1}>(services);",
                            reg.RequestType, reg.HandlerType));
                        break;
                    case "NH":
                        sb.AppendLine(string.Format(
                            "            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient<global::CQReetMediator.Abstractions.INotificationHandler<{0}>, {1}>(services);",
                            reg.RequestType, reg.HandlerType));
                        break;
                    case "PB":
                        if (reg.IsOpenGeneric)
                            sb.AppendLine(string.Format(
                                "            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(services, typeof(global::CQReetMediator.Abstractions.IPipelineBehavior<,>), typeof({0}));",
                                reg.HandlerType));
                        else
                            sb.AppendLine(string.Format(
                                "            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient<global::CQReetMediator.Abstractions.IPipelineBehavior<{0}, {1}>, {2}>(services);",
                                reg.RequestType, reg.ResponseType, reg.HandlerType));
                        break;
                    case "VPB":
                        if (reg.IsOpenGeneric)
                            sb.AppendLine(string.Format(
                                "            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(services, typeof(global::CQReetMediator.Abstractions.IPipelineBehavior<>), typeof({0}));",
                                reg.HandlerType));
                        else
                            sb.AppendLine(string.Format(
                                "            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient<global::CQReetMediator.Abstractions.IPipelineBehavior<{0}>, {1}>(services);",
                                reg.RequestType, reg.HandlerType));
                        break;
                }
            }

            sb.AppendLine();

            // --- Request wrappers ---
            var requestWrappers = distinct
                .Where(r => r.Kind == "RH")
                .GroupBy(r => r.RequestType)
                .Select(g => g.First())
                .ToList();

            sb.AppendLine(string.Format(
                "            var requestWrappers = new global::System.Collections.Generic.Dictionary<global::System.Type, object>({0});",
                requestWrappers.Count));
            foreach (var rw in requestWrappers)
            {
                sb.AppendLine(string.Format(
                    "            requestWrappers[typeof({0})] = new global::CQReetMediator.RequestWrapper<{0}, {1}>();",
                    rw.RequestType, rw.ResponseType));
            }

            sb.AppendLine();

            // --- Void request wrappers ---
            var voidWrappers = distinct
                .Where(r => r.Kind == "VRH")
                .GroupBy(r => r.RequestType)
                .Select(g => g.First())
                .ToList();

            sb.AppendLine(string.Format(
                "            var voidRequestWrappers = new global::System.Collections.Generic.Dictionary<global::System.Type, object>({0});",
                voidWrappers.Count));
            foreach (var vw in voidWrappers)
            {
                sb.AppendLine(string.Format(
                    "            voidRequestWrappers[typeof({0})] = new global::CQReetMediator.VoidRequestWrapper<{0}>();",
                    vw.RequestType));
            }

            sb.AppendLine();

            // --- Notification wrappers ---
            var notifWrappers = distinct
                .Where(r => r.Kind == "NH")
                .GroupBy(r => r.RequestType)
                .Select(g => g.First())
                .ToList();

            sb.AppendLine(string.Format(
                "            var notificationWrappers = new global::System.Collections.Generic.Dictionary<global::System.Type, global::CQReetMediator.NotificationWrapperBase>({0});",
                notifWrappers.Count));
            foreach (var nw in notifWrappers)
            {
                sb.AppendLine(string.Format(
                    "            notificationWrappers[typeof({0})] = new global::CQReetMediator.NotificationWrapper<{0}>();",
                    nw.RequestType));
            }

            sb.AppendLine();

            // --- Registry + Mediator ---
            sb.AppendLine(
                "            var registry = new global::CQReetMediator.MediatorRegistry(requestWrappers, voidRequestWrappers, notificationWrappers);");
            sb.AppendLine(
                "            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, registry);");
            sb.AppendLine(
                "            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::CQReetMediator.Abstractions.IMediator, global::CQReetMediator.Mediator>(services);");
            sb.AppendLine();
            sb.AppendLine("            return services;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }

    internal readonly struct RegistrationInfo : System.IEquatable<RegistrationInfo>
    {
        public readonly string Kind;
        public readonly string HandlerType;
        public readonly string RequestType;
        public readonly string ResponseType;
        public readonly bool IsOpenGeneric;

        public RegistrationInfo(string kind, string handlerType, string requestType, string responseType,
            bool isOpenGeneric)
        {
            Kind = kind;
            HandlerType = handlerType;
            RequestType = requestType;
            ResponseType = responseType;
            IsOpenGeneric = isOpenGeneric;
        }

        public bool Equals(RegistrationInfo other) =>
            Kind == other.Kind &&
            HandlerType == other.HandlerType &&
            RequestType == other.RequestType &&
            ResponseType == other.ResponseType &&
            IsOpenGeneric == other.IsOpenGeneric;

        public override bool Equals(object obj) => obj is RegistrationInfo other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (Kind != null ? Kind.GetHashCode() : 0);
                hash = hash * 31 + (HandlerType != null ? HandlerType.GetHashCode() : 0);
                hash = hash * 31 + (RequestType != null ? RequestType.GetHashCode() : 0);
                hash = hash * 31 + (ResponseType != null ? ResponseType.GetHashCode() : 0);
                hash = hash * 31 + IsOpenGeneric.GetHashCode();
                return hash;
            }
        }
    }
}