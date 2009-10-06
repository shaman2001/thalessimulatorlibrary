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

Imports ThalesSim.Core
Imports ThalesSim.Core.Message
Imports ThalesSim.Core.Cryptography

Namespace HostCommands

    ''' <summary>
    ''' This is the base class for all implementations of a Racal host command.
    ''' </summary>
    ''' <remarks>
    ''' All valid Racal host commands typically accept a request message and generate
    ''' a response message. Some commands that involve printer I/O return an additional
    ''' response message after printer I/O is concluded.
    ''' </remarks>
    Public Class AHostCommand

        ''' <summary>
        ''' 16 zeroes.
        ''' </summary>
        ''' <remarks>
        ''' 16 zeroes.
        ''' </remarks>
        Protected Const ZEROES As String = "0000000000000000"

        ''' <summary>
        ''' Suffix for X9.17 double-length key determiners.
        ''' </summary>
        ''' <remarks>
        ''' Suffix for X9.17 double-length key determiners.
        ''' </remarks>
        Protected Const DOUBLE_X917 As String = "_DOUBLE_X917"

        ''' <summary>
        ''' Suffix for X9.17 triple-length key determiners.
        ''' </summary>
        ''' <remarks>
        ''' Suffix for X9.17 triple-length key determiners.
        ''' </remarks>
        Protected Const TRIPLE_X917 As String = "_TRIPLE_X917"

        ''' <summary>
        ''' Suffix for variant double-length key determiners.
        ''' </summary>
        ''' <remarks>
        ''' Suffix for variant double-length key determiners.
        ''' </remarks>
        Protected Const DOUBLE_VARIANT As String = "_DOUBLE_VARIANT"

        ''' <summary>
        ''' Suffix for variant triple-length key determiners.
        ''' </summary>
        ''' <remarks>
        ''' Suffix for variant triple-length key determiners.
        ''' </remarks>
        Protected Const TRIPLE_VARIANT As String = "_TRIPLE_VARIANT"

        ''' <summary>
        ''' Suffix for double-length key determiners without a designation (X9.17).
        ''' </summary>
        ''' <remarks>
        ''' Suffix for double-length key determiners without a designation (X9.17).
        ''' </remarks>
        Protected Const PLAIN_DOUBLE As String = "_PLAIN_DOUBLE"

        ''' <summary>
        ''' Suffix for single-length key determiners without a designation (X9.17).
        ''' </summary>
        ''' <remarks>
        ''' Suffix for single-length key determiners without a designation (X9.17).
        ''' </remarks>
        Protected Const PLAIN_SINGLE As String = "_PLAIN_SIMPLE"

        ''' <summary>
        ''' Delimiter value.
        ''' </summary>
        ''' <remarks>
        ''' Common delimiter value used in some Racal commands.
        ''' </remarks>
        Protected Const DELIMITER_VALUE As String = ";"

        ''' <summary>
        ''' Key value for a delimiter field.
        ''' </summary>
        ''' <remarks>
        ''' Key value for a delimiter field.
        ''' </remarks>
        Protected Const DELIMITER As String = "DELIMITER"

        ''' <summary>
        ''' Key value to indicate delimiter presence.
        ''' </summary>
        ''' <remarks>
        ''' This value is used by a determiner if a delimiter is present.
        ''' </remarks>
        Protected Const DELIMITER_EXISTS As String = "DEL_PRESENT"

        ''' <summary>
        ''' Key value to indicate delimiter absence.
        ''' </summary>
        ''' <remarks>
        ''' This value is used by a determiner if a delimiter is absent.
        ''' </remarks>
        Protected Const DELIMITER_NOT_EXISTS As String = "DEL_ABSENT"

        ''' <summary>
        ''' Key value used for Key Scheme ZMK fields.
        ''' </summary>
        ''' <remarks>
        ''' Key value used for Key Scheme ZMK fields.
        ''' </remarks>
        Protected Const KEY_SCHEME_ZMK As String = "KEY_SCHEME_ZMK"

        ''' <summary>
        ''' Key value used for Key Scheme LMK fields.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Const KEY_SCHEME_LMK As String = "KEY_SCHEME_LMK"

        ''' <summary>
        ''' Key value used for Key Check Value fields.
        ''' </summary>
        ''' <remarks>
        ''' Key value used for Key Check Value fields.
        ''' </remarks>
        Protected Const KEY_CHECK_VALUE As String = "KEY_CHECK_VALUE"

        ''' <summary>
        ''' Format placeholder.
        ''' </summary>
        ''' <remarks>
        ''' This variable is used to hold the request message definition. At this level,
        ''' the request message is comprised of everything except the header and the
        ''' command code.
        ''' </remarks>
        Protected MFPC As ThalesSim.Core.Message.MessageFieldParserCollection

        ''' <summary>
        ''' Printer data.
        ''' </summary>
        ''' <remarks>
        ''' This variable is used to hold any data that the command directs to the 
        ''' attached printer.
        ''' </remarks>
        Protected _PrinterData As String = ""

        ''' <summary>
        ''' Returns data printed by this command.
        ''' </summary>
        ''' <remarks>
        ''' Returns data printed by this command. If the string is empty, no data have
        ''' been printed.
        ''' </remarks>
        Public ReadOnly Property PrinterData() As String
            Get
                Return _PrinterData
            End Get
        End Property

        ''' <summary>
        ''' Called to process a host request message.
        ''' </summary>
        ''' <remarks>
        ''' Override this method to perform request message parsing. The caller will call
        ''' this method before a call to <see cref="HostCommands.AHostCommand.ConstructResponse"/>.
        ''' </remarks>
        Public Overridable Sub AcceptMessage(ByVal msg As Message.Message)
        End Sub

        ''' <summary>
        ''' Called to return a response message.
        ''' </summary>
        ''' <remarks>
        ''' Override this method to create a response message. At this level, the response
        ''' message does <b>not</b> include the message header or the response code.
        ''' 
        ''' This method is called after <see cref="HostCommands.AHostCommand.AcceptMessage"/>.
        ''' </remarks>
        Public Overridable Function ConstructResponse() As Message.MessageResponse
            Return Nothing
        End Function

        ''' <summary>
        ''' Called to return a response message after performing printer I/O.
        ''' </summary>
        ''' <remarks>
        ''' Override this method to create a response message. At this level, the response
        ''' message does <b>not</b> include the message header or the response code. If
        ''' the specific host command is not supposed to perform printer I/O, return Nothing.
        ''' 
        ''' This method is called after <see cref="HostCommands.AHostCommand.ConstructResponse"/>.
        ''' </remarks>
        Public Overridable Function ConstructResponseAfterOperationComplete() As Message.MessageResponse
            Return Nothing
        End Function

        ''' <summary>
        ''' Called to perform cleanup.
        ''' </summary>
        ''' <remarks>
        ''' This method is called after <see cref="HostCommands.AHostCommand.ConstructResponseAfterOperationComplete"/>.
        ''' </remarks>
        Public Overridable Sub Terminate()
            MFPC = Nothing
        End Sub

        ''' <summary>
        ''' Returns a text dump of the parsed fields.
        ''' </summary>
        ''' <remarks>
        ''' This method returns a text dump of the message fields and their values.
        ''' </remarks>
        Public Function DumpFields() As String
            Dim s As String = ""
            For i As Integer = 0 To MFPC.MessageFieldCount - 1
                Dim o As Message.MessageFieldParser = MFPC.GetMessageFieldByIndex(i)
                s = s + "Field " + o.FieldName + ", value " + o.FieldValue + vbCrLf
            Next
            Return s
        End Function

        ''' <summary>
        ''' Parses and validates a key type code.
        ''' </summary>
        ''' <remarks>
        ''' This method parses a key type code. If all is well, the <b>Pair</b> and <b>Var</b> 
        ''' output variables are set and the method returns <b>True</b>. Otherwise, an appropriate
        ''' error code is added to the passed response message and the method returns <b>False</b>.
        ''' </remarks>
        Protected Function ValidateKeyTypeCode(ByVal ktc As String, _
                                               ByRef Pair As LMKPairs.LMKPair, _
                                               ByRef Var As String, _
                                               ByRef MR As MessageResponse) As Boolean

            Try
                KeyTypeTable.ParseKeyTypeCode(ktc, Pair, Var)
                Return True
            Catch ex As Exceptions.XInvalidKeyType
                MR.AddElement(ErrorCodes._04_INVALID_KEY_TYPE_CODE)
                Return False
            End Try

        End Function

        ''' <summary>
        ''' Parses and validates a key scheme code.
        ''' </summary>
        ''' <remarks>
        ''' This method parses a key scheme code. If all is well, the <b>KS</b> output variable
        ''' is set and the method returns <b>True</b>. Otherwise, an appropriate error code is
        ''' added to the passed response message and the method returns <b>False</b>.
        ''' </remarks>
        Protected Function ValidateKeySchemeCode(ByVal ksc As String, _
                                                 ByRef KS As KeySchemeTable.KeyScheme, _
                                                 ByRef MR As MessageResponse) As Boolean

            Try
                KS = KeySchemeTable.GetKeySchemeFromValue(ksc)
                Return True
            Catch ex As Exceptions.XInvalidKeyScheme
                MR.AddElement(ErrorCodes._26_INVALID_KEY_SCHEME)
                Return False
            End Try

        End Function

        ''' <summary>
        ''' Determines whether function requirements are met.
        ''' </summary>
        ''' <remarks>
        ''' If the function requirements are met for the specified parameters, the method returns
        ''' <b>True</b>. Otherwise, an appropriate error code is added to the passed response
        ''' message and the method returns <b>False</b>.
        ''' </remarks>
        Protected Function ValidateFunctionRequirement(ByVal func As KeyTypeTable.KeyFunction, ByVal Pair As LMKPairs.LMKPair, ByVal var As String, _
                                                       ByRef MR As MessageResponse) As Boolean

            Dim requirement As KeyTypeTable.AuthorizedStateRequirement = KeyTypeTable.GetAuthorizedStateRequirement(KeyTypeTable.KeyFunction.Generate, Pair, var)
            If requirement = KeyTypeTable.AuthorizedStateRequirement.NotAllowed Then
                MR.AddElement(ErrorCodes._29_FUNCTION_NOT_PERMITTED)
                Return False
            ElseIf requirement = KeyTypeTable.AuthorizedStateRequirement.NeedsAuthorizedState AndAlso CType(Resources.GetResource(Resources.AUTHORIZED_STATE), Boolean) = False Then
                MR.AddElement(ErrorCodes._17_HSM_IS_NOT_IN_THE_AUTHORIZED_STATE)
                Return False
            Else
                Return True
            End If

        End Function

        ''' <summary>
        ''' Generates a parser for multi-format keys.
        ''' </summary>
        ''' <remarks>
        ''' Several Thales commands have key inputs that have the form 16H or 1A+32H or 1A+48H. This
        ''' method creates an appropriate parser for this type of keys.
        ''' </remarks>
        Protected Function GenerateMultiKeyParser(ByVal keyName As String) As MessageFieldParser
            Dim MFDC_Key As New MessageFieldDeterminerCollection
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + DOUBLE_X917, _
                                                                   KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi), _
                                                                   32))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + TRIPLE_X917, _
                                                                   KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.TripleLengthKeyAnsi), _
                                                                   48))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + DOUBLE_VARIANT, _
                                                                   KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyVariant), _
                                                                   32))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + TRIPLE_VARIANT, _
                                                                   KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.TripleLengthKeyVariant), _
                                                                   48))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + PLAIN_SINGLE, "", 16))
            Return New MessageFieldParser(keyName, MFDC_Key)
        End Function

        ''' <summary>
        ''' Generates a parser for ZMK keys.
        ''' </summary>
        ''' <remarks>
        ''' Some Thales commands have ZMK key inputs that have the form 16H or 32H. This method
        ''' creates an appropriate parser for this type of key.
        ''' </remarks>
        Protected Function GenerateZMKKeyParser(ByVal keyName As String, ByVal msgLen As Integer) As MessageFieldParser
            Dim MFDC_Key As New MessageFieldDeterminerCollection
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + PLAIN_DOUBLE, msgLen, 32))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + PLAIN_SINGLE, "", 16))
            Return New MessageFieldParser(keyName, MFDC_Key)
        End Function

        ''' <summary>
        ''' Generates a parser for ZMK keys.
        ''' </summary>
        ''' <remarks>
        ''' Some Thales commands have ZMK key inputs of the form 16H or 32H or 1A+32H or 1A+48H.
        ''' This method creates an appropriate parser for this type of key.
        ''' </remarks>
        Protected Function GenerateLongZMKKeyParser(ByVal keyName As String, ByVal msgLen As Integer) As MessageFieldParser
            Dim MFDC_Key As New MessageFieldDeterminerCollection
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + DOUBLE_X917, _
                                                                   KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi), _
                                                                   32))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + TRIPLE_X917, _
                                                                   KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.TripleLengthKeyAnsi), _
                                                                   48))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + DOUBLE_VARIANT, _
                                                                   KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyVariant), _
                                                                   32))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + TRIPLE_VARIANT, _
                                                                   KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.TripleLengthKeyVariant), _
                                                                   48))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + PLAIN_DOUBLE, msgLen, 32))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + PLAIN_SINGLE, "", 16))
            Return New MessageFieldParser(keyName, MFDC_Key)
        End Function

        ''' <summary>
        ''' Generates a parser for a PVK pair.
        ''' </summary>
        ''' <remarks>
        ''' PVK parameters are of the form 32H or 1A+32H.
        ''' </remarks>
        Protected Function GeneratePVKKeyParser(ByVal keyName As String) As MessageFieldParser
            Dim MFDC_Key As New MessageFieldDeterminerCollection
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + DOUBLE_X917, _
                                                                   KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi), _
                                                                   32))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + DOUBLE_VARIANT, _
                                                                   KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyVariant), _
                                                                   32))
            MFDC_Key.AddFieldDeterminer(New MessageFieldDeterminer(keyName + PLAIN_DOUBLE, "", 32))
            Return New MessageFieldParser(keyName, MFDC_Key)
        End Function

        ''' <summary>
        ''' Generates parsers for common fields that follow a delimiter.
        ''' </summary>
        ''' <remarks>
        ''' This method generates several parsers that are used to parse a delimiter,
        ''' and optional key scheme ZMK, key scheme LMK and key check value characters.
        ''' </remarks>
        Protected Sub GenerateDelimiterParser()
            Dim MFDC_Del As New MessageFieldDeterminerCollection
            MFDC_Del.AddFieldDeterminer(New MessageFieldDeterminer(DELIMITER_EXISTS, 1, 1))
            MFDC_Del.AddFieldDeterminer(New MessageFieldDeterminer(DELIMITER_NOT_EXISTS, "", 0))
            Dim P_Del As MessageFieldParser = New MessageFieldParser(DELIMITER, MFDC_Del)
            MFPC.AddMessageFieldParser(P_Del)

            Dim P_ZmkScheme As New MessageFieldParser(KEY_SCHEME_ZMK, 1)
            P_ZmkScheme.DependentField = DELIMITER
            P_ZmkScheme.DependentValue = DELIMITER_VALUE
            MFPC.AddMessageFieldParser(P_ZmkScheme)

            Dim P_LmkScheme As New MessageFieldParser(KEY_SCHEME_LMK, 1)
            P_LmkScheme.DependentField = DELIMITER
            P_LmkScheme.DependentValue = DELIMITER_VALUE
            MFPC.AddMessageFieldParser(P_LmkScheme)

            Dim P_KeyCheckVal As New MessageFieldParser(KEY_CHECK_VALUE, 1)
            P_KeyCheckVal.DependentField = DELIMITER
            P_KeyCheckVal.DependentValue = DELIMITER_VALUE
            MFPC.AddMessageFieldParser(P_KeyCheckVal)
        End Sub

        ''' <summary>
        ''' Decrypts an ZMK.
        ''' </summary>
        ''' <remarks>
        ''' The ZMK is assumed to be a single-length key encrypted under LMK 04-05.
        ''' </remarks>
        Protected Function DecryptEncryptedZMK(ByVal encryptedKey As String) As String
            Return CStr(DecryptEncryptedZMK(encryptedKey, "", PLAIN_SINGLE))
        End Function

        ''' <summary>
        ''' Decrypts a ZMK.
        ''' </summary>
        ''' <remarks>
        ''' The ZMK type is determined by the determiner that matched the field.
        ''' </remarks>
        Protected Function DecryptEncryptedZMK(ByVal encryptedKey As String, ByVal keyPrefix As String, ByVal DeterminerName As String) As String
            Select Case DeterminerName
                Case keyPrefix + DOUBLE_VARIANT
                    Return DecryptZMKEncryptedUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, 0)
                Case keyPrefix + DOUBLE_X917
                    Return DecryptZMKEncryptedUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, 0)
                Case keyPrefix + TRIPLE_VARIANT
                    Return DecryptZMKEncryptedUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.TripleLengthKeyVariant, 0)
                Case keyPrefix + TRIPLE_X917
                    Return DecryptZMKEncryptedUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, 0)
                Case keyPrefix + PLAIN_DOUBLE
                    Return DecryptZMKEncryptedUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, 0)
                Case keyPrefix + PLAIN_SINGLE
                    Return DecryptZMKEncryptedUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.SingleDESKey, 0)
                Case Else
                    Throw New InvalidOperationException("Invalid key prefix [" + keyPrefix + "]")
            End Select
        End Function

        ''' <summary>
        ''' Decrypts a ZMK encrypted under LMK pair 04-05 and a variant.
        ''' </summary>
        ''' <remarks>
        ''' Decrypts a ZMK encrypted under LMK pair 04-05 and a variant.
        ''' </remarks>
        Private Shared Function DecryptZMKEncryptedUnderLMK(ByVal encryptedZMK As String, ByVal ks As Core.KeySchemeTable.KeyScheme, ByVal var As Integer) As String

            Select Case ks
                Case Core.KeySchemeTable.KeyScheme.SingleDESKey, Core.KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, Core.KeySchemeTable.KeyScheme.TripleLengthKeyAnsi
                    Return TripleDES.TripleDESDecrypt(New HexKey(Cryptography.LMK.LMKStorage.LMKVariant(Core.LMKPairs.LMKPair.Pair04_05, var)), encryptedZMK)
                Case Core.KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, Core.KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                    Return TripleDES.TripleDESDecryptVariant(New HexKey(Cryptography.LMK.LMKStorage.LMKVariant(Core.LMKPairs.LMKPair.Pair04_05, var)), encryptedZMK)
                Case Else
                    Throw New InvalidOperationException("Invalid key scheme [" + ks.ToString + "]")
            End Select

        End Function

        ''' <summary>
        ''' Encrypts clear data under a ZMK.
        ''' </summary>
        ''' <remarks>
        ''' This method may be used with Thales commands that encrypt key output under a ZMK.
        ''' </remarks>
        Protected Function EncryptUnderZMK(ByVal clearZMK As String, ByVal clearData As String, _
                                           ByVal ZMK_KeyScheme As KeySchemeTable.KeyScheme) As String

            Dim result As String = ""

            Select Case ZMK_KeyScheme
                Case KeySchemeTable.KeyScheme.SingleDESKey, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, KeySchemeTable.KeyScheme.Unspecified
                    result = TripleDES.TripleDESEncrypt(New HexKey(clearZMK), clearData)
                Case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                    result = TripleDES.TripleDESEncryptVariant(New HexKey(clearZMK), clearData)
            End Select

            Select Case ZMK_KeyScheme
                Case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                    result = KeySchemeTable.GetKeySchemeValue(ZMK_KeyScheme) + result
            End Select

            Return result

        End Function

        ''' <summary>
        ''' Decrypts data encrypted under a ZMK.
        ''' </summary>
        ''' <remarks>
        ''' This method may be used with Thales commands that decrypt key encrypted under a ZMK.
        ''' </remarks>
        Protected Function DecryptUnderZMK(ByVal clearZMK As String, ByVal cryptData As String, _
                                           ByVal ZMK_KeyScheme As KeySchemeTable.KeyScheme) As String
            Dim result As String = ""
            Select Case ZMK_KeyScheme
                Case KeySchemeTable.KeyScheme.SingleDESKey, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, KeySchemeTable.KeyScheme.Unspecified
                    result = TripleDES.TripleDESDecrypt(New HexKey(clearZMK), cryptData)
                Case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                    result = TripleDES.TripleDESDecryptVariant(New HexKey(clearZMK), cryptData)
            End Select

            Select Case ZMK_KeyScheme
                Case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                    result = KeySchemeTable.GetKeySchemeValue(ZMK_KeyScheme) + result
            End Select

            Return result
        End Function


        ''' <summary>
        ''' Returns a random hex key.
        ''' </summary>
        ''' <remarks>
        ''' Creates a single, double or triple length random hex key.
        ''' </remarks>
        Protected Function CreateRandomKey(ByVal ks As KeySchemeTable.KeyScheme) As String
            Select Case ks
                Case KeySchemeTable.KeyScheme.SingleDESKey
                    Return Utility.RandomKey(True, Utility.ParityCheck.OddParity)
                Case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant
                    Return Utility.MakeParity(Utility.RandomKey(False, Utility.ParityCheck.OddParity) + Utility.RandomKey(False, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity)
                Case KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                    Return Utility.MakeParity(Utility.RandomKey(False, Utility.ParityCheck.OddParity) + Utility.RandomKey(False, Utility.ParityCheck.OddParity) + Utility.RandomKey(False, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity)
                Case Else
                    Throw New InvalidOperationException("Invalid key scheme [" + ks.ToString + "]")
            End Select
        End Function

        ''' <summary>
        ''' Encrypts a key under an LMK pair and a variant.
        ''' </summary>
        ''' <remarks>
        ''' This method encrypts a key under an LMK pair.
        ''' </remarks>
        Protected Function EncryptUnderLMK(ByVal clearKey As String, _
                                           ByVal Target_KeyScheme As KeySchemeTable.KeyScheme, _
                                           ByVal LMKKeyPair As LMKPairs.LMKPair, _
                                           ByVal variantNumber As String) As String

            Dim result As String = ""

            Select Case Target_KeyScheme
                Case KeySchemeTable.KeyScheme.SingleDESKey, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, KeySchemeTable.KeyScheme.Unspecified
                    result = TripleDES.TripleDESEncrypt(New HexKey(LMK.LMKStorage.LMKVariant(LMKKeyPair, Convert.ToInt32(variantNumber))), clearKey)
                Case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                    result = TripleDES.TripleDESEncryptVariant(New HexKey(LMK.LMKStorage.LMKVariant(LMKKeyPair, Convert.ToInt32(variantNumber))), clearKey)
            End Select

            Select Case Target_KeyScheme
                Case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                    result = KeySchemeTable.GetKeySchemeValue(Target_KeyScheme) + result
            End Select

            Return result

        End Function

        ''' <summary>
        ''' Decrypts a key encrypted under an LMK pair and a variant.
        ''' </summary>
        ''' <remarks>
        ''' This method decrypts a key encrypted under an LMK pair and a variant.
        ''' </remarks>
        Protected Function DecryptUnderLMK(ByVal encryptedKey As String, _
                                           ByVal Target_KeyScheme As KeySchemeTable.KeyScheme, _
                                           ByVal LMKKeyPair As LMKPairs.LMKPair, _
                                           ByVal variantNumber As String) As String

            Dim result As String = ""

            Select Case Target_KeyScheme
                Case KeySchemeTable.KeyScheme.SingleDESKey, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, KeySchemeTable.KeyScheme.TripleLengthKeyAnsi
                    result = TripleDES.TripleDESDecrypt(New HexKey(LMK.LMKStorage.LMKVariant(LMKKeyPair, Convert.ToInt32(variantNumber))), encryptedKey)
                Case KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                    result = TripleDES.TripleDESDecryptVariant(New HexKey(LMK.LMKStorage.LMKVariant(LMKKeyPair, Convert.ToInt32(variantNumber))), encryptedKey)
            End Select

            Select Case Target_KeyScheme
                Case KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, KeySchemeTable.KeyScheme.TripleLengthKeyVariant
                    result = KeySchemeTable.GetKeySchemeValue(Target_KeyScheme) + result
            End Select

            Return result

        End Function

        ''' <summary>
        ''' Decrypts a key encrypted under an LMK pair and a variant.
        ''' </summary>
        ''' <remarks>
        ''' This method decrypts a key encrypted under an LMK pair and a variant. The key
        ''' length is found by the name of the determiner that matched the key.
        ''' </remarks>
        Protected Function DecryptUnderLMK(ByVal encryptedKey As String, _
                                   ByVal KeyPrefix As String, ByVal DeterminerName As String, _
                                   ByVal LMKKeyPair As LMKPairs.LMKPair, _
                                   ByVal variantNumber As String) As String

            Select Case DeterminerName
                Case KeyPrefix + DOUBLE_VARIANT
                    Return DecryptUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, LMKKeyPair, variantNumber)
                Case KeyPrefix + DOUBLE_X917
                    Return DecryptUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKKeyPair, variantNumber)
                Case KeyPrefix + TRIPLE_VARIANT
                    Return DecryptUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.TripleLengthKeyVariant, LMKKeyPair, variantNumber)
                Case KeyPrefix + TRIPLE_X917
                    Return DecryptUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, LMKKeyPair, variantNumber)
                Case KeyPrefix + PLAIN_DOUBLE
                    Return DecryptUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKKeyPair, variantNumber)
                Case KeyPrefix + PLAIN_SINGLE
                    Return DecryptUnderLMK(encryptedKey, KeySchemeTable.KeyScheme.SingleDESKey, LMKKeyPair, variantNumber)
                Case Else
                    Throw New InvalidOperationException("Invalid key prefix [" + KeyPrefix + "]")
            End Select

        End Function

        ''' <summary>
        ''' Creates a random PIN.
        ''' </summary>
        ''' <remarks>
        ''' This method creates a random decimal PIN.
        ''' </remarks>
        Protected Function GetRandomPIN(ByVal pinLength As Integer) As String
            Dim rndMachine As Random = New Random
            Dim PIN As String = ""
            For i As Integer = 1 To pinLength
                PIN += Convert.ToString(rndMachine.Next(0, 10))
            Next
            rndMachine = Nothing
            Return PIN
        End Function

        ''' <summary>
        ''' Encrypts a PIN.
        ''' </summary>
        ''' <remarks>
        ''' The current implementation only adds a 0 character to the clear PIN.
        ''' </remarks>
        Protected Function EncryptPINForHostStorage(ByVal PIN As String) As String
            Return "0" + PIN
        End Function

        ''' <summary>
        ''' Decrypts a PIN.
        ''' </summary>
        ''' <remarks>
        ''' The current implementation only removes the leading PIN character.
        ''' </remarks>
        Protected Function DecryptPINUnderHostStorage(ByVal PIN As String) As String
            Return PIN.Substring(1)
        End Function

        ''' <summary>
        ''' Encrypts a PIN using the Thales algorithm.
        ''' </summary>
        ''' <remarks>
        ''' The current implementation only adds a 0 character to the clear PIN.
        ''' </remarks>
        Protected Function EncryptPINForHostStorageThales(ByVal PIN As String) As String
            Return "0" + PIN
        End Function

        ''' <summary>
        ''' Decrypts a PIN encrypted with the Racal algorithm.
        ''' </summary>
        ''' <remarks>
        ''' The current implementation only removes the leading PIN character.
        ''' </remarks>
        Protected Function DecryptPINUnderHostStorageRacal(ByVal PIN As String) As String
            Return PIN.Substring(1)
        End Function

        ''' <summary>
        ''' Generates a VISA PVV.
        ''' </summary>
        ''' <remarks>
        ''' This method creates a 4-digit VISA PVV.
        ''' </remarks>
        Protected Function GeneratePVV(ByVal AccountNumber As String, ByVal PVKI As String, ByVal PIN As String, ByVal PVKPair As String) As String

            Dim stage1 As String = AccountNumber.Substring(0, 11) + PVKI + PIN.Substring(0, 4)
            Dim stage2 As String = TripleDES.TripleDESEncrypt(New HexKey(PVKPair), stage1)
            Dim PVV As String = "", i As Integer

            While PVV.Length < 4
                i = 0
                While PVV.Length < 4 AndAlso i < stage2.Length
                    If IsNumeric(stage2.Substring(i, 1)) Then PVV += stage2.Substring(i, 1)
                    i += 1
                End While
                If PVV.Length < 4 Then
                    For j As Integer = 0 To stage2.Length - 1
                        Dim newChar As String = " "
                        If IsNumeric(stage2.Substring(j, 1)) = False Then
                            newChar = Hex$(CLng("&H" + stage2.Substring(j, 1)) - 10)
                        End If
                        stage2 = stage2.Remove(j, 1)
                        stage2 = stage2.Insert(j, newChar)
                    Next
                    stage2 = stage2.Replace(" ", "")
                End If
            End While

            Return PVV

        End Function

        ''' <summary>
        ''' Generates a VISA CVV.
        ''' </summary>
        ''' <remarks>
        ''' This method generates a VISA CVV.
        ''' </remarks>
        Protected Function GenerateCVV(ByVal CVKPair As String, ByVal AccountNumber As String, ByVal ExpirationDate As String, ByVal SVC As String) As String

            Dim CVKA As String = Utility.RemoveKeyType(CVKPair).Substring(0, 16)
            Dim CVKB As String = Utility.RemoveKeyType(CVKPair).Substring(16)
            Dim block As String = (AccountNumber + ExpirationDate + SVC).PadRight(32, "0"c)
            Dim blockA As String = block.Substring(0, 16)
            Dim blockB As String = block.Substring(16)

            Dim result As String = TripleDES.TripleDESEncrypt(New HexKey(CVKA), blockA)
            result = Utility.XORHexStrings(result, blockB)
            result = TripleDES.TripleDESEncrypt(New HexKey(CVKA + CVKB), result)

            Dim CVV As String = "", i As Integer = 0
            While CVV.Length < 3
                If IsNumeric(result.Substring(i, 1)) Then
                    CVV += result.Substring(i, 1)
                End If
                i += 1
            End While

            Return CVV

        End Function

        ''' <summary>
        ''' Generates a MAC.
        ''' </summary>
        ''' <remarks>
        ''' Generates a message authentication code.
        ''' </remarks>
        Protected Function GenerateMAC(ByVal b() As Byte, ByVal key As String, ByVal IV As String) As String

            Dim curIndex As Integer = 0
            Dim result As String = ""

            While curIndex <= b.GetUpperBound(0)
                MACBytes(b, curIndex, IV, key, result)
                IV = result
            End While

            Return result

        End Function

        Private Sub MACBytes(ByVal b() As Byte, ByRef curIndex As Integer, ByVal IV As String, ByVal key As String, ByRef result As String)

            Dim dataStr As String = ""

            While dataStr.Length <> 16
                If curIndex <= b.GetUpperBound(0) Then
                    dataStr = dataStr + Hex$(b(curIndex)).PadLeft(2, "0"c)
                    curIndex += 1
                Else
                    dataStr = dataStr.PadRight(16, "0"c)
                End If
            End While

            dataStr = Utility.XORHexStringsFull(dataStr, IV)
            result = TripleDES.TripleDESEncrypt(New HexKey(key), dataStr)

        End Sub

        ''' <summary>
        ''' Adds a line to the printer output.
        ''' </summary>
        ''' <remarks>
        ''' Adds a line to the printer output.
        ''' </remarks>
        Protected Sub AddPrinterData(ByVal data As String)
            _PrinterData = PrinterData + data + vbCrLf
        End Sub

        ''' <summary>
        ''' Clears the printer output.
        ''' </summary>
        ''' <remarks>
        ''' Clears the printer output.
        ''' </remarks>
        Protected Sub ClearPrinterData()
            _PrinterData = ""
        End Sub

    End Class



End Namespace
