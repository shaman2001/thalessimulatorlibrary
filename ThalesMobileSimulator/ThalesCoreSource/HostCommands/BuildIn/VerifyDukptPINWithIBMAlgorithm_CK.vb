﻿''
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
' Contributed by rjw - May 2010

Imports ThalesSim.Core.Message
Imports ThalesSim.Core.Cryptography
Imports ThalesSim.Core
Imports ThalesSim.Core.PIN.PINBlockFormat
Imports ThalesSim.Core.Cryptography.DUKPT

Namespace HostCommands.BuildIn

    ''' <summary>
    ''' Verifies a DUKPT PIN using the IBM algorithm.
    ''' </summary>
    ''' <remarks>
    ''' This class implements the CK Thales command.
    ''' </remarks>
    <ThalesCommandCode("CK", "CL", "", "Verifies a DUKPT PIN using the IBM algorithm")> _
    Public Class VerifyDukptPINWithIBMAlgorithm_CK
        Inherits AHostCommand

        Private _acct As String
        Private _ksnDescriptor As String
        Private _ksn As String
        Private _pinBlock As String
        Private _pvkPair As String
        Private _checkLen As String
        Private _bdk As String
        Private _decTable As String
        Private _pinValData As String
        Private _offsetValue As String
        Private _expPinValData As String
        Dim _cryptAcctNum As String
        Dim _decimalisedAcctNum As String
        Dim _naturalPin As String
        Dim _derivedPin As String

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <remarks>
        ''' The constructor sets up the CK message parsing fields.
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
                _bdk = kvp.ItemCombination("BDK Scheme", "BDK")
                _pvkPair = kvp.ItemCombination("PVK Scheme", "PVK")
                _ksnDescriptor = kvp.Item("KSN Descriptor")
                _ksn = kvp.Item("Key Serial Number")
                _pinBlock = kvp.Item("Encrypted Block")
                _checkLen = kvp.Item("Check Length")
                _acct = kvp.Item("Account Number")
                _decTable = kvp.Item("Decimalisation Table")
                _pinValData = kvp.Item("PIN Validation")
                _offsetValue = kvp.Item("Offset")
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

            Dim cryptBDK As New HexKey(_bdk)
            Dim clearBDK As String = Utility.DecryptUnderLMK(cryptBDK.ToString, cryptBDK.Scheme, LMKPairs.LMKPair.Pair28_29, "0")
            If Utility.IsParityOK(clearBDK, Utility.ParityCheck.OddParity) = False Then
                mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR)
                Return mr
            End If

            Dim cryptPVK As New HexKey(_pvkPair)
            Dim clearPVK As String = Utility.DecryptUnderLMK(cryptPVK.ToString, cryptPVK.Scheme, LMKPairs.LMKPair.Pair14_15, "0")
            If Utility.IsParityOK(clearPVK, Utility.ParityCheck.OddParity) = False Then
                mr.AddElement(ErrorCodes.ER_11_DESTINATION_KEY_PARITY_ERROR)
                Return mr
            End If

            If Convert.ToInt16(_checkLen) < 4 Then
                mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA)
                Return mr
            End If

            Dim ksn As New KeySerialNumber(_ksn, _ksnDescriptor)
            Dim dukptKey As String = DerivedKey.calculateDerivedKey(ksn, clearBDK)
            Dim clearPB As String = TripleDES.TripleDESDecrypt(New HexKey(dukptKey), _pinBlock)
            Dim clearPIN As String = ToPIN(clearPB, _acct, PIN_Block_Format.AnsiX98)

            If clearPIN.Length < 4 OrElse clearPIN.Length > 12 Then
                mr.AddElement(ErrorCodes.ER_24_PIN_IS_FEWER_THAN_4_OR_MORE_THAN_12_DIGITS_LONG)
                Return mr
            End If

            _expPinValData = _pinValData.Substring(0, _pinValData.IndexOf("N"))
            _expPinValData = _expPinValData + _acct.Substring(_acct.Length - 5, 5)
            _expPinValData = _expPinValData + _pinValData.Substring(_pinValData.IndexOf("N") + 1, (_pinValData.Length - (_pinValData.IndexOf("N") + 1)))
            _cryptAcctNum = DES.DESEncrypt(clearPVK, _expPinValData)

            _decimalisedAcctNum = Utility.Decimalise(_cryptAcctNum, _decTable)
            _naturalPin = _decimalisedAcctNum.Substring(0, Convert.ToInt32(_checkLen))
            _derivedPin = Utility.AddNoCarry(_naturalPin, _offsetValue.Substring(0, _offsetValue.IndexOf("F")))

            If _derivedPin <> clearPIN Then
                mr.AddElement(ErrorCodes.ER_01_VERIFICATION_FAILURE)
            Else
                mr.AddElement(ErrorCodes.ER_00_NO_ERROR)
            End If

            Return mr

        End Function

    End Class

End Namespace
