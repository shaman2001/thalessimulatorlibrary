''
'' This program is free software; you can redistribute it and/or modify
'' it under the terms of the GNU General Public License as published by
'' the Free Software Foundation; either version 2 of the License, or
'' (at your option) any later version.
''
'' This program is distributed in the hope that it will be useful,
'' but WITHOUT ANY WARRANTY; without even the implied warranty of
'' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'' GNU General Public License for more details.
''
'' You should have received a copy of the GNU General Public License
'' along with this program; if not, write to the Free Software
'' Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
'' 

Imports ThalesSim.Core.Message
Imports ThalesSim.Core.Cryptography
Imports ThalesSim.Core

Namespace HostCommands.BuildIn

    ''' <summary>
    ''' Generates two random keys.
    ''' </summary>
    ''' <remarks>
    ''' This class implements the FG Racal command.
    ''' </remarks>
    <ThalesCommandCode("FG", "FH", "", "Generates two random keys.")> _
    Public Class GeneratePVKPair_FG
        Inherits AHostCommand

        Private _sourceZmk As String
        Private _del As String
        Private _keySchemeZMK As String
        Private _keySchemeLMK As String
        Private _keyCheckValue As String

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <remarks>
        ''' The constructor sets up the FG message parsing fields.
        ''' </remarks>
        Public Sub New()
            ReadXMLDefinitions()
        End Sub

        ''' <summary>
        ''' Parses the request message.
        ''' </summary>
        ''' <remarks>
        ''' This method parses the command message. The message header and message command
        ''' code are <b>not</b> part of the message.
        ''' </remarks>
        Public Overrides Sub AcceptMessage(ByVal msg As Message.Message)
            XML.MessageParser.Parse(msg, XMLMessageFields, kvp, XMLParseResult)
            If XMLParseResult = ErrorCodes.ER_00_NO_ERROR Then
                _sourceZmk = kvp.ItemCombination("ZMK Scheme", "ZMK")
                _del = kvp.ItemOptional("Delimiter")
                _keySchemeZMK = kvp.ItemOptional("Key Scheme ZMK")
                _keySchemeLMK = kvp.ItemOptional("Key Scheme LMK")
                _keyCheckValue = kvp.ItemOptional("Key Check Value Type")
            End If
        End Sub

        ''' <summary>
        ''' Creates the response message.
        ''' </summary>
        ''' <remarks>
        ''' This method creates the response message. The message header and message reply code
        ''' are <b>not</b> part of the message.
        ''' </remarks>
        Public Overrides Function ConstructResponse() As Message.MessageResponse
            Dim mr As New MessageResponse

            Dim ks As KeySchemeTable.KeyScheme, zmkKs As KeySchemeTable.KeyScheme

            If _del = DELIMITER_VALUE Then
                If ValidateKeySchemeCode(_keySchemeLMK, ks, mr) = False Then Return mr
                If ValidateKeySchemeCode(_keySchemeZMK, zmkKs, mr) = False Then Return mr

                If ks <> KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi AndAlso ks <> KeySchemeTable.KeyScheme.DoubleLengthKeyVariant Then
                    mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME)
                    Return mr
                End If

                If zmkKs <> KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi AndAlso zmkKs <> KeySchemeTable.KeyScheme.DoubleLengthKeyVariant Then
                    mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME)
                    Return mr
                End If

            Else
                ks = KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi
                zmkKs = KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi
                _keyCheckValue = "0"
            End If

            Dim clearSource As String

            Dim cryptZMK As New HexKey(_sourceZmk)
            clearSource = Utility.DecryptUnderLMK(cryptZMK.ToString, cryptZMK.Scheme, LMKPairs.LMKPair.Pair04_05, "0")
            If Utility.IsParityOK(clearSource, Utility.ParityCheck.OddParity) = False Then
                mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR)
                Return mr
            End If

            Dim clearKey As String = Utility.CreateRandomKey(ks)

            Dim cryptKeyZMK As String = Utility.EncryptUnderZMK(clearSource, clearKey, zmkKs)
            Dim cryptKeyLMK As String = Utility.EncryptUnderLMK(clearKey, ks, LMKPairs.LMKPair.Pair14_15, "0")
            Dim chkVal1 As String = TripleDES.TripleDESEncrypt(New HexKey(clearKey), ZEROES)
            Dim chkVal2 As String = TripleDES.TripleDESEncrypt(New HexKey(Utility.RemoveKeyType(clearKey).Substring(0, 16)), ZEROES)
            Dim chkVal3 As String = TripleDES.TripleDESEncrypt(New HexKey(Utility.RemoveKeyType(clearKey).Substring(16)), ZEROES)

            Log.Logger.MinorInfo("ZMK (clear): " + clearSource)
            Log.Logger.MinorInfo("PVK (clear): " + clearKey)
            Log.Logger.MinorInfo("PVK (ZMK): " + cryptKeyZMK)
            Log.Logger.MinorInfo("PVK (LMK): " + cryptKeyLMK)
            Log.Logger.MinorInfo("Check Value (full key): " + chkVal1)
            Log.Logger.MinorInfo("Check Value (left): " + chkVal2)
            Log.Logger.MinorInfo("Check Value (right): " + chkVal3)

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR)

            If _keySchemeLMK = "" Then
                mr.AddElement(Utility.RemoveKeyType(cryptKeyZMK))
                mr.AddElement(Utility.RemoveKeyType(cryptKeyLMK))
            Else
                mr.AddElement(cryptKeyZMK)
                mr.AddElement(cryptKeyLMK)
            End If

            If _keyCheckValue = "0" Then
                mr.AddElement(chkVal2)
                mr.AddElement(chkVal3)
            ElseIf _keyCheckValue = "2" Then
                mr.AddElement(chkVal2.Substring(0, 6))
                mr.AddElement(chkVal3.Substring(0, 6))
            Else
                mr.AddElement(chkVal1.Substring(0, 6))
            End If

            Return mr

        End Function

    End Class

End Namespace

