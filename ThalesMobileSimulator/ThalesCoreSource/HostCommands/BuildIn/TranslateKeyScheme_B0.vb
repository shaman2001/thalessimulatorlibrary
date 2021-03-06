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
    ''' Translates an existing key to a new key scheme.
    ''' </summary>
    ''' <remarks>
    ''' This class implements the B0 Racal command.
    ''' </remarks>
    <ThalesCommandCode("B0", "B1", "", "Translates an existing key to a new key scheme")> _
    Public Class TranslateKeyScheme_B0
        Inherits AHostCommand

        Private _keyType As String = ""
        Private _keyScheme As String = ""
        Private _key As String = ""

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <remarks>
        ''' The constructor sets up the B0 message parsing fields.
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
                _key = kvp.ItemCombination("Key Scheme", "Key")
                _keyType = kvp.Item("Key Type")
                _keyScheme = kvp.Item("Key Scheme (LMK)")
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

            Dim LMKKeyPair As LMKPairs.LMKPair, var As String = ""
            Dim ks As KeySchemeTable.KeyScheme

            If Not IsInAuthorizedState() Then
                Log.Logger.MajorInfo("Can't print clear key while not in the AUTHORIZED state")
                mr.AddElement(ErrorCodes.ER_17_HSM_IS_NOT_IN_THE_AUTHORIZED_STATE)
                Return mr
            End If

            If ValidateKeyTypeCode(_keyType, LMKKeyPair, var, mr) = False Then Return mr
            If ValidateKeySchemeCode(_keyScheme, ks, mr) = False Then Return mr

            Dim cryptSourceKey As New HexKey(_key)
            Dim clearKey As String = Utility.DecryptUnderLMK(cryptSourceKey.ToString, cryptSourceKey.Scheme, LMKKeyPair, var)
            If Utility.IsParityOK(clearKey, Utility.ParityCheck.OddParity) = False Then
                mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR)
                Return mr
            End If

            clearKey = Utility.RemoveKeyType(clearKey)

            Select Case ks
                Case KeySchemeTable.KeyScheme.SingleDESKey
                    If clearKey.Length > 16 Then
                        mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME)
                        Return mr
                    End If
                Case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant
                    If clearKey.Length <> 32 Then
                        mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME)
                        Return mr
                    End If
                Case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                    If clearKey.Length <> 48 Then
                        mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME)
                        Return mr
                    End If
            End Select

            Dim cryptKey As String = Utility.EncryptUnderLMK(clearKey, ks, LMKKeyPair, var)

            Log.Logger.MinorInfo("Key (clear): " + clearKey)
            Log.Logger.MinorInfo("Key (LMK): " + cryptKey)

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR)
            mr.AddElement(cryptKey)

            Return mr

        End Function

    End Class

End Namespace
