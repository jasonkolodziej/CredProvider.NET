using CredProvider.NET.Interop2;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using static CredProvider.NET.Constants;

namespace CredProvider.NET
{

    /// <summary>
    /// Exposes methods that enable the handling of a credential through <see cref="ICredentialProviderCredential"/>: 
    ///     is implemented by outside parties providing a Logon UI or Credential UI prompting for user credentials. 
    ///     Enumeration of user tiles cannot be done without an implementation of this interface.
    /// and adds a method that retrieves the security identifier (SID) of a user. 
    /// The credential is associated with that user and can be grouped under the user's tile.
    /// </summary>
    /// <remarks>
    /// This class is required for creating a V2 credential provider. V2 credential providers provide a personalized log on experience for the user.
    /// This occurs by the credential provider telling the Logon UI what sign in options are available for a user. 
    /// It is recommended that new credential providers should be V2 credential providers.
    /// In order to create an <see cref="ICredentialProviderCredential2"/> instance, 
    /// a valid SID needs to be returned by the <see cref="CredentialProviderCredential.GetUserSid"/> function.
    /// Valid is defined by the returned SID being for one of the users currently enumerated by the Logon UI.
    /// A useful tool for getting the available users and determining which ones you want to associate with is the <see cref="ICredentialProviderUserArray"/> object.
    /// This object contains a list of <see cref="ICredentialProviderUser"/> objects that can be queried to gain information about the users that will be enumerated.
    /// For example you could gain the user's SID or username using <see cref="CredentialProviderCredential.GetStringValue"/> with a passed in parameter of 
    /// PKEY_Identity_PrimarySid or PKEY_Identity_USerName respectively.
    /// You can even filter the results using SetProviderFilter to only display a subset of available users.
    /// Using the <see cref="ICredentialProviderUserArray"/> is optional, but it is a convenient way to get the necessary information to make valid SID values.
    /// In order to get a list of users that will be enumerated by the Logon UI, implement the <see cref="ICredentialProviderSetUserArray"/> interface to get the ICredentialProviderUserArray object from SetUserArray.
    /// Logon UI calls SetUserArray before GetCredentialCount, so the ICredentialProviderUserArray object is ready when a credential provider is about to return credentials.
    /// A V2 credential provider is represented by an icon displayed underneath the "Sign-in options" link.
    /// In order to provide an icon for your credential provider, define a CREDENTIAL_PROVIDER_FIELD_TYPE of CPFT_TILE_IMAGE in the credential itself.
    /// Then make sure the guidFieldType of the CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR is set to CPFG_CREDENTIAL_PROVIDER_LOGO.The recommended size for an icon is 72 by 72 pixels.
    /// Similar to specifying an icon for your credential provider, you can also specify a text string to identify your credential provider.
    /// This string appears in a pop-up window when a user hovers over the icon.To do this, define a CREDENTIAL_PROVIDER_FIELD_TYPE of CPFT_SMALL_TEXT in the credential itself.
    /// Then make sure the guidFieldType of the CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR is set to CPFG_CREDENTIAL_PROVIDER_LABEL.
    /// This string should supplement the credential provider icon described above and be descriptive enough that users understand what it is. 
    /// For example, the picture password provider's description is "Picture Password".
    /// </remarks>
    public class CredentialProviderCredential : ICredentialProviderCredential2
    {
        private readonly CredentialView view;
        private string sid;

        public CredentialProviderCredential(CredentialView view, string sid)
        {
            Logger.Write();

            this.view = view;
            this.sid = sid;
        }
        /// <summary>
        /// Enables a credential to initiate events in the Logon UI or Credential UI through a callback interface. 
        /// This method should be called before other methods in ICredentialProviderCredential interface.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-advise"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="pcpce">A pointer to an ICredentialProviderCredentialEvents callback interface to be used as the notification mechanism.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int Advise(ICredentialProviderCredentialEvents pcpce)
        {
            Logger.Write();

            if (pcpce is ICredentialProviderCredentialEvents2 ev2)
            {
                Logger.Write("pcpce is ICredentialProviderCredentialEvents2");
            }

            return HRESULT.S_OK;
        }
        /// <summary>
        /// Used by the Logon UI or Credential UI to advise the credential that event callbacks are no longer accepted.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-unadvise"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int UnAdvise()
        {
            Logger.Write();

            return HRESULT.E_NOTIMPL;
        }
        /// <summary>
        /// Called when a credential is selected. Enables the implementer to set logon characteristics.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-setselected"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="pbAutoLogon">
        ///     When this method returns, contains TRUE if selection of the credential indicates that it should attempt to logon immediately and automatically, otherwise FALSE. 
        ///     For example, a credential provider that enumerates an account without a password may want to return this as true.
        /// </param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int SetSelected(out int pbAutoLogon)
        {
            Logger.Write();

            //Set this to 1 if you would like GetSerialization called immediately on selection
            pbAutoLogon = 0;

            return HRESULT.S_OK;
        }
        /// <summary>
        /// Called when a credential loses selection.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-setdeselected"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int SetDeselected()
        {
            Logger.Write();

            return HRESULT.E_NOTIMPL;
        }
        /// <summary>
        /// Retrieves the field state. The Logon UI and Credential UI use this to gain information about a field of a credential to display this information in the user tile.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-getfieldstate"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="dwFieldID">The identifier for the field.</param>
        /// <param name="pcpfs">A pointer to the credential provider field state. This indicates when the field should be displayed on the user tile.</param>
        /// <param name="pcpfis">A pointer to the credential provider field interactive state. This indicates when the user can interact with the field.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetFieldState(
            uint dwFieldID,
            out _CREDENTIAL_PROVIDER_FIELD_STATE pcpfs,
            out _CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE pcpfis
        )
        {
            Logger.Write($"dwFieldID: {dwFieldID}");

            view.GetFieldState((int)dwFieldID, out pcpfs, out pcpfis);

            return HRESULT.S_OK;
        }
        /// <summary>
        /// Enables retrieval of text from a credential with a text field.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-getstringvalue"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="dwFieldID">The identifier for the field.</param>
        /// <param name="ppsz">A pointer to the memory containing a null-terminated Unicode string to return to the Logon UI or Credential UI.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetStringValue(uint dwFieldID, out string ppsz)
        {
            Logger.Write($"dwFieldID: {dwFieldID}");

            ppsz = view.GetValue((int)dwFieldID);
            
            return HRESULT.S_OK;
        }

        private Bitmap tileIcon;
        /// <summary>
        /// Enables retrieval of bitmap data from a credential with a bitmap field.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-getbitmapvalue"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="dwFieldID">The identifier for the field.</param>
        /// <param name="phbmp">Contains a pointer to the handle of the bitmap.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetBitmapValue(uint dwFieldID, out IntPtr phbmp)
        {
            Logger.Write($"dwFieldID: {dwFieldID}");

            try
            {
                TryLoadUserIcon();
            }
            catch (Exception ex) 
            {
                Logger.Write("Error: " + ex);
            }

            phbmp = tileIcon?.GetHbitmap() ?? IntPtr.Zero;

            return HRESULT.S_OK;
        }
        /// <summary>
        /// Helps load the CP Logo Icon
        /// </summary>
        private void TryLoadUserIcon()
        {
            if (tileIcon == null)
            {
                var fileName = "CredProvider.NET.tile-icon.bmp";
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream(fileName);

                tileIcon = (Bitmap)Image.FromStream(stream);
            }
        }
        /// <summary>
        /// Retrieves the checkbox value.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-getcheckboxvalue"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="dwFieldID">The identifier for the field.</param>
        /// <param name="pbChecked">Indicates the state of the checkbox. TRUE indicates the checkbox is checked, otherwise FALSE.</param>
        /// <param name="ppszLabel">Points to the label on the checkbox.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetCheckboxValue(uint dwFieldID, out int pbChecked, out string ppszLabel)
        {
            Logger.Write($"dwFieldID: {dwFieldID}");

            pbChecked = 0;
            ppszLabel = "";

            return HRESULT.E_NOTIMPL;
        }
        /// <summary>
        /// Retrieves the identifier of a field that the submit button should be placed next to in the Logon UI.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-getsubmitbuttonvalue"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="dwFieldID">The identifier for the field a submit button value is needed for.</param>
        /// <param name="pdwAdjacentTo">A pointer to a value that receives the field ID of the field that the submit button should be placed next to.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetSubmitButtonValue(uint dwFieldID, out uint pdwAdjacentTo)
        {
            Logger.Write($"dwFieldID: {dwFieldID}");

            pdwAdjacentTo = 0;

            return HRESULT.E_NOTIMPL;
        }
        /// <summary>
        /// Gets a count of the items in the specified combo box and designates which item should have initial selection.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-getcomboboxvaluecount"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="dwFieldID">The identifier for the combo box to gather information about.</param>
        /// <param name="pcItems">A pointer to the number of items in the given combo box.</param>
        /// <param name="pdwSelectedItem">Contains a pointer to the item that receives initial selection.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetComboBoxValueCount(uint dwFieldID, out uint pcItems, out uint pdwSelectedItem)
        {
            Logger.Write($"dwFieldID: {dwFieldID}");

            pcItems = 0;
            pdwSelectedItem = 0;

            return HRESULT.E_NOTIMPL;
        }
        /// <summary>
        /// Gets the string label for a combo box entry at the given index.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-getcomboboxvalueat"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="dwFieldID">The identifier for the combo box to query.</param>
        /// <param name="dwItem">The index of the desired item.</param>
        /// <param name="ppszItem">A pointer to the string value that receives the combo box label.</param>
        /// <returns></returns>
        public virtual int GetComboBoxValueAt(uint dwFieldID, uint dwItem, out string ppszItem)
        {
            Logger.Write($"dwFieldID: {dwFieldID}; dwItem: {dwItem}");

            ppszItem = "";

            return HRESULT.E_NOTIMPL;
        }
        /// <summary>
        /// Enables a Logon UI or Credential UI to update the text for a CPFT_EDIT_TEXT fields as the user types in them.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-setstringvalue"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="dwFieldID">The identifier for the field that needs to be updated.</param>
        /// <param name="psz">A pointer to a buffer containing the new text.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int SetStringValue(uint dwFieldID, string psz)
        {
            Logger.Write($"dwFieldID: {dwFieldID}; psz: {psz}");

            view.SetValue((int) dwFieldID, psz);

            return HRESULT.S_OK;
        }
        /// <summary>
        /// Enables a Logon UI and Credential UI to indicate that a checkbox value has changed.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-setcheckboxvalue"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="dwFieldID">The identifier for the field to update.</param>
        /// <param name="bChecked">Indicates the new value for the checkbox. TRUE means the checkbox should be checked, FALSE means unchecked.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int SetCheckboxValue(uint dwFieldID, int bChecked)
        {
            Logger.Write($"dwFieldID: {dwFieldID}; bChecked: {bChecked}");

            return HRESULT.E_NOTIMPL;
        }
        /// <summary>
        /// Enables a Logon UI and Credential UI to indicate that a combo box value has been selected.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-setcomboboxselectedvalue"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="dwFieldID">The identifier for the combo box that is affected.</param>
        /// <param name="dwSelectedItem">The specific item selected.</param>
        /// <returns><see cref="HRESULT"/>returns>
        public virtual int SetComboBoxSelectedValue(uint dwFieldID, uint dwSelectedItem)
        {
            Logger.Write($"dwFieldID: {dwFieldID}; dwSelectedItem: {dwSelectedItem}");

            return HRESULT.E_NOTIMPL;
        }
        /// <summary>
        /// Enables the Logon UI and Credential UI to indicate that a link was clicked.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-commandlinkclicked"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="dwFieldID">The identifier for the field clicked on.</param>
        /// <returns></returns>
        public virtual int CommandLinkClicked(uint dwFieldID)
        {
            Logger.Write($"dwFieldID: {dwFieldID}");

            return HRESULT.E_NOTIMPL;
        }
        /// <summary>
        /// Called in response to an attempt to submit this credential to the underlying authentication engine.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-getserialization"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="pcpgsr">Indicates the success or failure of the attempt to serialize credentials.</param>
        /// <param name="pcpcs"></param>
        /// <param name="ppszOptionalStatusText">A pointer to the credential. Depending on the result, there may be no valid credential.</param>
        /// <param name="pcpsiOptionalStatusIcon">A pointer to a Unicode string value that will be displayed by the Logon UI after serialization. May be NULL.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetSerialization(
            out _CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE pcpgsr,
            out _CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION pcpcs,
            out string ppszOptionalStatusText,
            out _CREDENTIAL_PROVIDER_STATUS_ICON pcpsiOptionalStatusIcon
        )
        {
            Logger.Write();

            var usage = this.view.Provider.GetUsage();

            pcpgsr = _CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE.CPGSR_NO_CREDENTIAL_NOT_FINISHED;
            pcpcs = new _CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION();
            ppszOptionalStatusText = "";
            pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_NONE;

            //Serialization can be called before the user has entered any values. Only applies to logon usage scenarios
            if (usage == _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_LOGON || usage == _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_UNLOCK_WORKSTATION)
            {
                //Determine the authentication package
                Common.RetrieveNegotiateAuthPackage(out var authPackage);

                //Only credential packing for msv1_0 is supported using this code
                Logger.Write($"Got authentication package: {authPackage}. Only local authenticsation package 0 (msv1_0) is supported.");

                //Get username and password
                var username = Common.GetNameFromSid(this.sid);
                GetStringValue(2, out var password);

                Logger.Write($"Preparing to serialise credential with password...");
                pcpgsr = _CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE.CPGSR_RETURN_CREDENTIAL_FINISHED;
                pcpcs = new _CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION();

                var inCredSize = 0;
                var inCredBuffer = Marshal.AllocCoTaskMem(0);

                //This should work fine in Windows 10 that only uses the Logon scenario
                //But it could fail for the workstation unlock scanario on older OS's
                if (!PInvoke.CredPackAuthenticationBuffer(0, username, password, inCredBuffer, ref inCredSize))
                {
                    Marshal.FreeCoTaskMem(inCredBuffer);
                    inCredBuffer = Marshal.AllocCoTaskMem(inCredSize);

                    if (PInvoke.CredPackAuthenticationBuffer(0, username, password, inCredBuffer, ref inCredSize))
                    {
                        ppszOptionalStatusText = string.Empty;
                        pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_SUCCESS;

                        //Better to move the CLSID to a constant (but currently used in the .reg file)
                        pcpcs.clsidCredentialProvider = Guid.Parse("00006d50-0000-0000-b090-00006b0b0000");
                        pcpcs.rgbSerialization = inCredBuffer;
                        pcpcs.cbSerialization = (uint)inCredSize;
                        pcpcs.ulAuthenticationPackage = authPackage;

                        return HRESULT.S_OK;
                    }

                    ppszOptionalStatusText = "Failed to pack credentials";
                    pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_ERROR;
                    return HRESULT.E_FAIL;
                }
            }
            //Implement code to change password here. This is not handled natively.
            else if (usage == _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_CHANGE_PASSWORD)
            {
                pcpgsr = _CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE.CPGSR_NO_CREDENTIAL_FINISHED;
                pcpcs = new _CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION();
                ppszOptionalStatusText = "Password changed success message.";
                pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_SUCCESS;
            }

            Logger.Write("Returning S_OK");
            return HRESULT.S_OK;
        }
        /// <summary>
        /// Translates a received error status code into the appropriate user-readable message.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovidercredential-reportresult"/>
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential"/></remarks>
        /// <param name="ntsStatus">The NTSTATUS value that reflects the return value of the Winlogon call to LsaLogonUser.</param>
        /// <param name="ntsSubstatus">The NTSTATUS value that reflects the value pointed to by the SubStatus parameter of LsaLogonUser when that function returns after being called by Winlogon.</param>
        /// <param name="ppszOptionalStatusText">A pointer to the error message that will be displayed to the user. May be NULL.</param>
        /// <param name="pcpsiOptionalStatusIcon">A pointer to an icon that will shown on the credential. May be NULL.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int ReportResult(
            int ntsStatus,
            int ntsSubstatus,
            out string ppszOptionalStatusText,
            out _CREDENTIAL_PROVIDER_STATUS_ICON pcpsiOptionalStatusIcon
        )
        {
            Logger.Write($"ntsStatus: {ntsStatus}; ntsSubstatus: {ntsSubstatus}");

            ppszOptionalStatusText = "";
            pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_NONE;

            return HRESULT.S_OK;
        }

        /// <summary>
        /// Retrieves the security identifier (SID) of the user that is associated with this credential.
        /// </summary>
        /// <remarks>Inherited through: <see cref="ICredentialProviderCredential2"/></remarks>
        /// <param name="sid">The address of a pointer to a buffer that, when this method returns successfully, receives the user's SID.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetUserSid(out string sid)
        {
            Logger.Write();

            sid = this.sid;

            Console.WriteLine($"Returning sid: {sid}");
            return HRESULT.S_OK;
        }
    }
}