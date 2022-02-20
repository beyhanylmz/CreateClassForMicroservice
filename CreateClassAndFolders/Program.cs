using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CreateClassAndFolders
{
   class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter EntityName \n");
            var entityName = Console.ReadLine();

            Console.Write("Enter ProjectRoot \n");
            var projectRoot = Console.ReadLine();
            
            CreateFolderAndClass(entityName, projectRoot);
            Console.Read();
        }

        /// <summary>
        /// Create related folders.
        /// </summary>
        static void CreateFolderAndClass(string entityName, string projectRoot)
        {
            const string relatedDirectory = "Features";
            const string projectName = "ProjectName.Account";

            //Create Directory For The Entity under the Project Feature Folder,
            var entityFeatureFolderRoot = Path.Combine(projectRoot, relatedDirectory, entityName);

            if (!Directory.Exists(entityFeatureFolderRoot))
            {
                Directory.CreateDirectory(entityFeatureFolderRoot);
            }

            //Create Directories For the Entity Under Feature Under Entity
            var fileNameList = new[] {"Commands", "Queries", "BusinessRules"};
            foreach (var item in fileNameList)
            {
                var itemPath = Path.Combine(entityFeatureFolderRoot, item);

                if (!Directory.Exists(itemPath))
                {
                    Directory.CreateDirectory(itemPath);
                }

                //Create Directory "ValidationRules" for Command and Query 
                if (item != "BusinessRules")
                {
                    var validationRulePath =
                        Path.Combine(Path.Combine(entityFeatureFolderRoot, item, "ValidationRules"));
                    if (!Directory.Exists(validationRulePath))
                    {
                        Directory.CreateDirectory(validationRulePath);
                    }
                }
            }

            var commandNames = new[]
            {
                "Create", "Update", "Delete"
            };
            var queryNames = new[]
            {
                "GetById", "GetAll"
            };
            foreach (var command in commandNames)
            {
                //Create Commands Class for the Entity
                CreateCQClassUnderFeature(command, entityName, true, entityFeatureFolderRoot, projectName);

                //Create ValidationRules of the Command Class
                CreateValidationRulesClassUnderFeature(command, entityName, true, entityFeatureFolderRoot, projectName);
            }

            foreach (var query in queryNames)
            {
                //Create Query Class for the Entity
                CreateCQClassUnderFeature(query, entityName, false, entityFeatureFolderRoot, projectName);

                //Create ValidatonRules of the Query Class
                CreateValidationRulesClassUnderFeature(query, entityName, false, entityFeatureFolderRoot, projectName);
            }

            //Create Mapping Class of the Entity
            CreateMappingClass(entityName, projectRoot, projectName);

            //Create DTO ResponseModel Class of the Entity
            CreateDTOClass(entityName, projectRoot, projectName);
        }

        private static void CreateCQClassUnderFeature(string crudName, string entityName, bool isCommand,
            string entityFeatureFolderRoot, string projectName)
        {
            var commandOrQueryPlural = isCommand ? "Commands" : "Queries";
            var commandOrQueryText = isCommand ? "Command" : "Query";

            //Add Using
            var usingsyntax = new[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(projectName + ".Wrappers")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(projectName + ".DTOs")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("ProjectName.Business")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("MediatR")),
            };

            // Create a namespace: namespace ProjectName.Account.Features.Branch.Commands
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory
                    .ParseName(projectName + ".Features." + entityName + "." + commandOrQueryPlural)
                    .NormalizeWhitespace())
                .AddUsings(usingsyntax);

            //  Create a class: (class CreateBranchCommanmd)
            var className = isCommand
                ? (crudName + entityName + commandOrQueryText)
                : (entityName + crudName + commandOrQueryText);

            var firstClassDeclaration = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IRequest<IResult>")));

            // Add the class to the namespace.
            @namespace = @namespace.AddMembers(firstClassDeclaration);

            //  Create second class: (class CreateBranchCommandHandler)
            var secondClassDeclaration = SyntaxFactory.ClassDeclaration(className + "Handler")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(
                    SyntaxFactory.SimpleBaseType(
                        SyntaxFactory.ParseTypeName("IRequestHandler<" + className + ", IResult>")));

            // Create a statement with the body of a method.
            var syntax = SyntaxFactory.ParseStatement(
                "return new Response<ByNameResponseModel>(new ByNameResponseModel {Name = new" + entityName +
                ".Name},\"İşlem Başarılı\");");

            var param = SyntaxFactory.Parameter(
                    SyntaxFactory.Identifier("request"))
                .WithType(SyntaxFactory.ParseTypeName(className));

            var param2 = SyntaxFactory.Parameter(
                    SyntaxFactory.Identifier("cancellationToken"))
                .WithType(SyntaxFactory.ParseTypeName(typeof(CancellationToken).FullName));

            var paramList = new[]
            {
                param, param2
            };

            // Create a method
            var methodDeclaration = SyntaxFactory
                .MethodDeclaration(SyntaxFactory.ParseTypeName("async Task<IResult>"), "Handle")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(paramList)
                .WithBody(SyntaxFactory.Block(syntax));


            secondClassDeclaration = secondClassDeclaration.AddMembers(methodDeclaration);

            var fileName = className + ".cs";
            var filePath = Path.Combine(entityFeatureFolderRoot, commandOrQueryPlural, fileName);

            AddClassToTheNameSpaceAndCreate(filePath, @namespace, secondClassDeclaration);
        }

        private static void CreateValidationRulesClassUnderFeature(string crudName, string entityName, bool isCommand,
            string entityFeatureFolderRoot, string projectName)
        {
            var commandOrQueryPlural = isCommand ? "Commands" : "Queries";
            var commandOrQueryText = isCommand ? "Command" : "Query";

            //Add Using
            var usingsyntax = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("FluentValidation"));

            // Create a namespace: namespace ProjectName.Account.Features.Branch.Commands
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(projectName + ".Features." +
                    entityName + "." + commandOrQueryPlural +
                    ".ValidationRules")
                .NormalizeWhitespace()).AddUsings(usingsyntax);

            //  Create a class: (class CreateBranchCommanmd)
            var className = isCommand
                ? (crudName + entityName + commandOrQueryText)
                : (entityName + crudName + commandOrQueryText);

            var classDeclaration = SyntaxFactory.ClassDeclaration(className + "Validator")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(
                    SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("AbstractValidator<" + className + ">")));

            //Constructor Method and one sample
            var syntax = SyntaxFactory.ParseStatement(
                " RuleFor(x => x.Name).NotEmpty().MaximumLength(50);");
            var methodDeclaration = SyntaxFactory
                .MethodDeclaration(SyntaxFactory.ParseTypeName(""), className + "Validator")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(SyntaxFactory.Block(syntax));

            classDeclaration = classDeclaration.AddMembers(methodDeclaration);

            var fileName = className + "Validator" + ".cs";
            var filePath = Path.Combine(entityFeatureFolderRoot, commandOrQueryPlural, "ValidationRules", fileName);

            AddClassToTheNameSpaceAndCreate(filePath, @namespace, classDeclaration);
        }

        private static void CreateMappingClass(string entityName, string projectRoot, string projectName)
        {
            var mappingDirectoryForEntity = Path.Combine(projectRoot, "Mappings", entityName);

            if (!Directory.Exists(mappingDirectoryForEntity))
            {
                Directory.CreateDirectory(mappingDirectoryForEntity);
            }

            //Add Using
            var usingsyntax = new[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("AutoMapper")),
                SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName(projectName + ".Features." + entityName + ".Commands")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(projectName + ".DTOs." + entityName))
            };

            var @namespace = SyntaxFactory
                .NamespaceDeclaration(
                    SyntaxFactory.ParseName(projectName + ".Mappings." + entityName).NormalizeWhitespace())
                .AddUsings(usingsyntax);

            //  Create a class: (class BranchMappingProfile)
            var className = entityName + "MappingProfile";

            var classDeclaration = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("Profile")));

            //Constructor Method and default mappings for the project
            var syntax = SyntaxFactory.ParseStatement(
                " CreateMap<Create" + entityName + "Command, Domain.Entity." + entityName +
                ">().ForMember(x => x.CreateDate,expression => expression.MapFrom(mapExpression => DateTime.Now));" +
                " CreateMap<Update" + entityName + "Command, Domain.Entity." + entityName +
                ">().ForMember(x => x.UpdateDate,expression => expression.MapFrom(mapExpression => DateTime.Now));" +
                " CreateMap<Domain.Entity." + entityName + ", " + entityName + "ResponseModel>();");

            var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(""), className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(SyntaxFactory.Block(syntax));

            classDeclaration = classDeclaration.AddMembers(methodDeclaration);

            var fileName = className + ".cs";
            var filePath = Path.Combine(mappingDirectoryForEntity, fileName);

            AddClassToTheNameSpaceAndCreate(filePath, @namespace, classDeclaration);
        }

        private static void CreateDTOClass(string entityName, string projectRootPath, string projectName)
        {
            var dtoDirectoryForEntity = Path.Combine(projectRootPath, "DTOs", entityName);

            if (!Directory.Exists(dtoDirectoryForEntity))
            {
                Directory.CreateDirectory(dtoDirectoryForEntity);
            }

            //Add Using 
            var usingsyntax = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System"));

            var @namespace = SyntaxFactory.NamespaceDeclaration(
                    SyntaxFactory.ParseName(projectName + ".DTOs." + entityName).NormalizeWhitespace())
                .AddUsings(usingsyntax);

            //  Create a class: (class BranchResponseModel)

            var className = entityName + "ResponseModel";

            var classDeclaration = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            var fileName = className + ".cs";
            var filePath = Path.Combine(dtoDirectoryForEntity, fileName);

            AddClassToTheNameSpaceAndCreate(filePath, @namespace, classDeclaration);
        }

        private static void AddClassToTheNameSpaceAndCreate(string filePath,
            NamespaceDeclarationSyntax @namespace, ClassDeclarationSyntax classDeclaration)
        {
            // Add the class to the namespace.
            @namespace = @namespace.AddMembers(classDeclaration);

            // Normalize and get code as string.
            var code = @namespace
                .NormalizeWhitespace()
                .ToFullString();

            if (!File.Exists(filePath))
            {
                var fileStream = File.Create(filePath);
                fileStream.Dispose();
                File.WriteAllText(filePath, code);

                Console.WriteLine(code);
            }
        }
    }
}
