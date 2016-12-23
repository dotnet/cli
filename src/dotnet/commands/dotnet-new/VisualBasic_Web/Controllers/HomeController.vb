Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Mvc

Namespace MvcApp.Controllers
    Public Class HomeController
        Inherits Controller

        Public Function Index() As IActionResult
            Return View()
        End Function

        Public Function About() As IActionResult
            ViewData("Message") = "Your application description page."

            Return View()
        End Function

        Public Function Contact() As IActionResult
            ViewData("Message") = "Your contact page."

            Return View()
        End Function

        Public Function [Error]() As IActionResult
            Return View()
        End Function
    End Class
End Namespace
