Imports System
Imports Newtonsoft.Json

Namespace Test2
    Class Program
        Private Shared Sub print()
            Dim macbook = New Computer With {
                .Vendor = "apple Inc",
                .produceDate = New DateTime(),
                .price = "1200$"
            }
            Dim json As String = JsonConvert.SerializeObject(macbook)
            Console.WriteLine("my new computer is {0}", json)
        End Sub

        Public Class Computer
            Public Property Vendor As String
            Public Property produceDate As DateTime
            Public Property price As String
        End Class
    End Class
End Namespace
