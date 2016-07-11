using System;
using System.Reflection;

namespace MadeInTheUSB.NusbioDevice.WebClient.Areas.HelpPage.ModelDescriptions
{
    public interface IModelDocumentationProvider
    {
        string GetDocumentation(MemberInfo member);

        string GetDocumentation(Type type);
    }
}