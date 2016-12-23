Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Hosting

Namespace MvcApp
    Public Class Program
        Sub Main()
            Dim host As New WebHostBuilder().
                UseKestrel().
                UseContentRoot(Directory.GetCurrentDirectory()).
                UseIISIntegration().
                UseStartup(Of Startup)().
                Build()

            host.Run()
        End Sub
    End Class
End Namespace
