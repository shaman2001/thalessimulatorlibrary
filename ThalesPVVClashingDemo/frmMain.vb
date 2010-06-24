﻿Imports System.Windows.Forms
Imports ThalesSim
Imports ThalesSim.Core

Public Class frmMain

    Dim WithEvents thales As TCP.WorkerClient
    Dim thalesData As String = ""

    Private Sub frmMain_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        My.Settings.PIN = txtPIN.Text
        My.Settings.PAN = txtPAN.Text
        My.Settings.clearTPK = txtClearTPK.Text
        My.Settings.encryptedTPK = txtCryptTPK.Text
        My.Settings.encryptedPVK = txtCryptPVK.Text
        My.Settings.thalesIP = txtIPAddress.Text
        My.Settings.thalesPort = txtPort.Text
        My.Settings.Save()
    End Sub

    Private Sub cmdFindAllPINs_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdFindAllPINs.Click
        Me.Enabled = False

        Try
            thales = New TCP.WorkerClient(New Net.Sockets.TcpClient(txtIPAddress.Text, Convert.ToInt32(txtPort.Text)))
            thales.InitOps()
        Catch ex As Exception
            doLog("Connection error (" + ex.Message + ")")
            Me.Enabled = True
            Exit Sub
        End Try

        txtLog.Text = ""

        doLog("Finding PVV for PIN " + txtPIN.Text + "...")
        Dim key As New Cryptography.HexKey(txtClearTPK.Text)
        Dim PB As String = Cryptography.TripleDES.TripleDESEncrypt(key, txtPIN.Text + New String("F"c, 12))
        Dim acctNumber As String = txtPAN.Text.Substring(txtPAN.Text.Length - 13, 12)

        Dim reply As String = SendFunctionCommand("1234JC" + txtCryptTPK.Text + PB + "03" + acctNumber)
        If reply.Substring(6, 2) = "00" Then
            reply = SendFunctionCommand("1234DG" + txtCryptPVK.Text + reply.Substring(8, 5) + acctNumber + "1")
            If reply.Substring(6, 2) = "00" Then

                Dim PVV As String = reply.Substring(8, 4)
                doLog("PVV is " + PVV)
                doLog("Running PIN verification for all PINs and this PVV...")
                For i As Integer = 0 To 9999
                    PB = Cryptography.TripleDES.TripleDESEncrypt(key, i.ToString.PadLeft(4, "0"c) + New String("F"c, 12))
                    reply = SendFunctionCommand("1234DC" + txtCryptTPK.Text + txtCryptPVK.Text + PB + "03" + acctNumber + "1" + PVV)
                    If reply.Substring(6, 2) = "00" Then
                        doLog("Verified for PIN [" + i.ToString.PadLeft(4, "0"c) + "]")
                    End If
                    If i Mod 50 = 0 Then
                        lblStatus.Text = "Running verification for PIN #" + i.ToString.PadLeft(4, "0"c) + "..."
                        Application.DoEvents()
                    End If
                Next

                lblStatus.Text = "Done."
            Else
                doLog("Error on DG: " + reply)
            End If
        Else
            doLog("Error on JC: " + reply)
        End If

        thales.TermClient()
        thales = Nothing
        Me.Enabled = True
    End Sub

    Private Function SendFunctionCommand(ByVal s As String) As String
        thalesData = ""
        thales.send(s)

        While thalesData = "" AndAlso thales.IsConnected
            Threading.Thread.Sleep(1)
        End While

        If Not thales.IsConnected Then
            Return ""
        Else
            Return thalesData
        End If
    End Function

    Private Sub thales_Disconnected(ByVal sender As ThalesSim.Core.TCP.WorkerClient) Handles thales.Disconnected
        doLog("Disconnected from HSM")
    End Sub

    Private Sub thales_MessageArrived(ByVal sender As ThalesSim.Core.TCP.WorkerClient, ByRef b() As Byte, ByVal len As Integer) Handles thales.MessageArrived
        Dim s As String = ""
        For i As Integer = 0 To len - 1
            s = s + Chr(b(i))
        Next
        thalesData = s
    End Sub

    Private Sub doLog(ByVal s As String)
        txtLog.AppendText(s + vbCrLf)
        txtLog.ScrollToCaret()
    End Sub

    Private Sub frmMain_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        test()
    End Sub

    Private Sub test()
        Dim IMK_AC As New Core.Cryptography.HexKey("9E15204313F7318ACB79B90BD986AD29")

        Dim PAN As String = "5364140000000367"
        Dim PAN_Sequence_No As String = "01"

        Dim MK_AC As Core.Cryptography.HexKey = GetDerivedKey_OptionA(IMK_AC, PAN.Substring(2), PAN_Sequence_No)
        Debug.WriteLine(MK_AC.ToString)

        Dim ATC As String = "0007"
        Dim UN As String = "00000000" '"53446578"

        Dim SK_AC As Core.Cryptography.HexKey = FindSessionKey(MK_AC, ATC, UN)
        Debug.WriteLine(SK_AC.ToString)

        Dim Amount_Authorized As String = "000000000000"
        Dim Amount_Other As String = "000000000000"
        Dim Terminal_Country_Code As String = "0000"
        Dim TVR As String = "8000000000"
        Dim Transaction_Currency_Code As String = "0000"
        Dim Transaction_Date As String = "000000"
        Dim Transaction_Type As String = "00"
        Dim AIP As String = "0000"
        Dim CVR As String = "800001240000"

        Dim AC As String = GetMac(Amount_Authorized + Amount_Other + Terminal_Country_Code + TVR + Transaction_Currency_Code + Transaction_Date + Transaction_Type + UN + AIP + ATC + CVR, SK_AC)
        Debug.WriteLine(AC)

        Dim TDS_0 As String = "4725"
        Dim TDS_1 As String = "123456789"
        Dim TDS_2 As String = "120704"

        Dim TDSMAC As Core.Cryptography.HexKey = New Core.Cryptography.HexKey(GetMac(PadWithNibbles(TDS_0, TDS_1, TDS_2), New Core.Cryptography.HexKey(AC + AC)))
        Debug.WriteLine(TDSMAC.ToString)


    End Sub

    Private Function PadWithNibbles(ByVal first As String, ByVal second As String, ByVal third As String) As String
        Dim s As String = first + "F" + second + "F" + third + "F"
        If s.Length Mod 2 = 1 Then
            s = s + "F"
        End If
        Return s
    End Function

    ''' <summary>
    ''' Calculates a MAC on the track data using the derived key.
    ''' </summary>
    ''' <param name="data">Track data used for calculation.</param>
    ''' <param name="KD">Derived key.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetMac(ByVal data As String, ByVal KD As Cryptography.HexKey) As String
        'If it's padded to 8-byte blocks, add another required block.
        If data.Length Mod 16 = 0 Then
            data = data + "8000000000000000"
        Else
            'Else, add 0x80 and pad to an 8-byte block with zeroes.
            data = data + "80"
            If data.Length Mod 16 <> 0 Then
                data = data.PadRight(data.Length + (16 - data.Length Mod 16), "0"c)
            End If
        End If

        'With an initial 8-byte block of zeroes and starting from the left of the track data:
        '  1. XOR the initial block with the next 8-byte block of the track data.
        '  2. Encrypt the result with the left side of the key.
        '  3. Use the result of the above and go to 1 until all track data are processed.
        '  4. Decrypt the result of the above with the right side of the key.
        '  5. Encrypt the result of the above with the left side of the key.
        '  6. The 2 least significant bytes of the result are the MAC.
        Dim KL As New Cryptography.HexKey(KD.ToString.Substring(0, 16))
        Dim startIndex As Integer = 0
        Dim intermediateryData As String = "0000000000000000"
        While startIndex <= data.Length - 1
            Dim algoData As String = data.Substring(startIndex, 16)
            startIndex += 16
            intermediateryData = Utility.XORHexStringsFull(intermediateryData, algoData)
            intermediateryData = Cryptography.TripleDES.TripleDESEncrypt(KL, intermediateryData)
        End While
        Dim KR As New Cryptography.HexKey(KD.ToString.Substring(16))
        intermediateryData = Cryptography.TripleDES.TripleDESDecrypt(KR, intermediateryData)
        intermediateryData = Cryptography.TripleDES.TripleDESEncrypt(KL, intermediateryData)
        Return intermediateryData.Substring(0, 16)
    End Function

    Private Function FindSessionKey(ByVal MK_AC As Core.Cryptography.HexKey, ByVal ATC As String, ByVal UN As String) As Core.Cryptography.HexKey
        Return New Core.Cryptography.HexKey(Core.Cryptography.TripleDES.TripleDESEncrypt(MK_AC, ATC + "F000" + UN) + Core.Cryptography.TripleDES.TripleDESEncrypt(MK_AC, ATC + "0F00" + UN))
    End Function

    ''' <summary>
    ''' Calculates the derived key using the initial key, the PAN and the sequence number.
    ''' </summary>
    ''' <param name="IMK">Initial key.</param>
    ''' <param name="PAN">PAN.</param>
    ''' <param name="PANSequenceNo">PAN sequence number.</param>
    ''' <returns></returns>
    ''' <remarks>This implements the key derivation method A.</remarks>
    Private Function GetDerivedKey_OptionA(ByVal IMK As Cryptography.HexKey, ByVal PAN As String, ByVal PANSequenceNo As String) As Cryptography.HexKey
        'Add sequence number to PAN and pad to at least 16 digits. 
        'Then get the rightmost sixteen digits to form an 8-byte block.
        Dim Y As String = PAN + PANSequenceNo
        If Y.Length < 16 Then
            Y = Y.PadLeft(16, "0"c)
        End If
        Y = Y.Substring(Y.Length - 16)

        Return GetDerivedKeyFromPreparedPAN(IMK, Y)
    End Function

    ''' <summary>
    ''' Calculates the derived key using the initial key, the PAN and the sequence number.
    ''' This method is called when the PAN is larger than 16 digits.
    ''' </summary>
    ''' <param name="IMK">Initial key.</param>
    ''' <param name="PAN">PAN.</param>
    ''' <param name="PANSequenceNo">PAN sequence number.</param>
    ''' <returns></returns>
    ''' <remarks>This implements the key derivation method B.</remarks>

    Private Function GetDerivedKey_OptionB(ByVal IMK As Cryptography.HexKey, ByVal PAN As String, ByVal PANSequenceNo As String) As Cryptography.HexKey
        Dim Y As String = PAN + PANSequenceNo

        'Pad to an even length.
        If PAN.Length Mod 2 = 1 Then
            Y = Y + "0"c
        End If

        'Hash Y.
        Dim hash As Security.Cryptography.HashAlgorithm = New Security.Cryptography.SHA1Managed
        Dim result() As Byte = hash.ComputeHash(System.Text.ASCIIEncoding.GetEncoding(Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage).GetBytes(Y))

        'Get hex result.
        Dim resultStr As String = ""
        Utility.ByteArrayToHexString(result, resultStr)

        'Keep values A, B, C, D, E and F here.
        Dim undecimalized As String = ""

        'Try to get to the first 16 decimal characters.
        Y = ""
        For i As Integer = 0 To resultStr.Length - 1
            If Char.IsDigit(resultStr.Chars(i)) Then
                Y = Y + resultStr.Chars(i)
                If Y.Length = 16 Then Exit For
            Else
                undecimalized = undecimalized + resultStr.Chars(i)
            End If
        Next

        'If more are needed, do the decimalization and get the rest.
        If Y.Length < 16 Then
            Dim decimalized As String = Utility.Decimalise(undecimalized, "012345")
            Y = Y + decimalized.Substring(0, 16 - Y.Length)
        End If

        Return GetDerivedKeyFromPreparedPAN(IMK, Y)
    End Function


    ''' <summary>
    ''' Calculates the derived key using the initial key given a massaged PAN.
    ''' </summary>
    ''' <param name="IMK">Initial key.</param>
    ''' <param name="Y">Prepared PAN from OptionA or OptionB methods.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetDerivedKeyFromPreparedPAN(ByVal IMK As Cryptography.HexKey, ByVal Y As String) As Cryptography.HexKey
        'Left key is the result of encrypting Y with the IMK.
        Dim ZL As String = Cryptography.TripleDES.TripleDESEncrypt(IMK, Y)
        'Right key is the result of encrypting Y XOR FFs with the IMK.
        Dim ZR As String = Cryptography.TripleDES.TripleDESEncrypt(IMK, Utility.XORHexStringsFull(Y, "FFFFFFFFFFFFFFFF"))

        'Left+Right = the derived key.
        Return New Cryptography.HexKey(Utility.MakeParity(ZL + ZR, Utility.ParityCheck.OddParity))
    End Function


End Class