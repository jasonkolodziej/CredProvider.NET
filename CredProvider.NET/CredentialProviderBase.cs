using CredProvider.NET.Interop2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static CredProvider.NET.Constants;

namespace CredProvider.NET
{
    /// <summary>
    /// Exposes methods used in the setup and manipulation of a credential provider. All credential providers must implement this interface.
    /// </summary>
    /// <seealso cref="CredentialProvider"/>
    /// <remarks>
    /// This interface is how you will interact with the Logon UI and the Credential UI for your app.
    /// An instantiated credential provider is maintained for the entire lifetime of a Logon UI.
    /// Because of this, the Logon UI can maintain the state of a credential provider.
    /// In particular, it remembers which provider and tile provided a credential.
    /// This means that you can potentially store state information when you are using a <see cref="_CREDENTIAL_PROVIDER_USAGE_SCENARIO"/> of 
    ///     CPUS_LOGON, CPUS_UNLOCK_WORKSTATION, and CPUS_CHANGE_PASSWORD.
    /// This is not the case with the Credential UI. The Credential UI creates a new instance of the provider every time an application calls CredUIPromptForWindowsCredentials.
    /// Because of this, the Credential UI cannot remember a credential provider's state.
    /// Be aware that a CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION generated in one scenario might be saved and used in a subsequent usage scenario. 
    /// Because of this, it is necessary to make sure your ICredentialProvider implementation is robust enough to handle this scenario.
    /// Windows 8+ adds new functionality in the credential providers API, primarily the ability to group credentials by user.
    /// </remarks>
    public abstract class CredentialProviderBase : ICredentialProvider, ICredentialProviderSetUserArray
    {
        private ICredentialProviderEvents events;

        protected abstract CredentialView Initialize(_CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus, uint dwFlags);

        private CredentialView view;
        private _CREDENTIAL_PROVIDER_USAGE_SCENARIO usage;

        private List<ICredentialProviderUser> providerUsers;

        /// <summary>
        /// Defines the scenarios for which the credential provider is valid. Called whenever the credential provider is initialized.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovider-setusagescenario"/>
        /// </summary>
        /// <param name="cpus">The scenario the credential provider has been created in. This is the usage scenario that needs to be supported. See the Remarks for more information.</param>
        /// <param name="dwFlags">A value that affects the behavior of the credential provider. This value can be a bitwise-OR combination of one or more of the following values defined in Wincred.h. See CredUIPromptForWindowsCredentials for more information.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int SetUsageScenario(_CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus, uint dwFlags)
        {
            view = Initialize(cpus, dwFlags);
            usage = cpus;

            if (view.Active)
            {
                return HRESULT.S_OK;
            }

            return HRESULT.E_NOTIMPL;
        }
        /// <summary>
        /// Sets the serialization characteristics of the credential provider.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovider-setserialization"/>
        /// </summary>
        /// <param name="pcpcs">A pointer to a <see cref="_CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION"/> structure that stores the serialization characteristics of the credential provider.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int SetSerialization(ref _CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION pcpcs)
        {
            Logger.Write($"ulAuthenticationPackage: {pcpcs.ulAuthenticationPackage}");

            return HRESULT.S_OK;
        }

        /// <summary>
        /// Allows a credential provider to initiate events in the Logon UI or Credential UI through a callback interface.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovider-advise"/>
        /// </summary>
        /// <param name="pcpe">A pointer to an <see cref="ICredentialProviderEvents"/> callback interface to be used as the notification mechanism.</param>
        /// <param name="upAdviseContext">A pointer to an <see cref="ICredentialProviderEvents"/> callback interface to be used as the notification mechanism.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int Advise(ICredentialProviderEvents pcpe, ulong upAdviseContext)
        {
            Logger.Write($"upAdviseContext: {upAdviseContext}");

            if (pcpe != null)
            {
                events = pcpe;

                Marshal.AddRef(Marshal.GetIUnknownForObject(pcpe));
            }

            return HRESULT.S_OK;
        }
        /// <summary>
        /// Used by the Logon UI or Credential UI to advise the credential provider that event callbacks are no longer accepted.
        /// </summary>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int UnAdvise()
        {
            Logger.Write();

            if (events != null)
            {
                //Marshal.Release(Marshal.GetIUnknownForObject(events));
                events = null;
            }

            return HRESULT.S_OK;
        }
        /// <summary>
        /// Retrieves the count of fields in the needed to display this provider's credentials.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovider-getfielddescriptorcount"/>
        /// </summary>
        /// <param name="pdwCount">Pointer to the field count.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetFieldDescriptorCount(out uint pdwCount)
        {
            Logger.Write();

            pdwCount = (uint)view.DescriptorCount;

            Logger.Write($"Returning field count: {pdwCount}");

            return HRESULT.S_OK;
        }
        /// <summary>
        /// Gets metadata that describes a specified field.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovider-getfielddescriptorat"/>
        /// </summary>
        /// <param name="dwIndex">The zero-based index of the field for which the information should be retrieved.</param>
        /// <param name="ppcpfd">The address of a pointer to a <see cref="_CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR"/> structure which receives the information about the field.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetFieldDescriptorAt(uint dwIndex, [Out] IntPtr ppcpfd)
        {
            if (view.GetField((int)dwIndex, ppcpfd))
            {
                return HRESULT.S_OK;
            }

            return HRESULT.E_INVALIDARG;
        }

        /// <summary>
        /// Gets the number of available credentials under this credential provider.
        /// <inheritdoc path="https://docs.microsoft.com/en-us/windows/win32/api/credentialprovider/nf-credentialprovider-icredentialprovider-getcredentialcount"/>
        /// </summary>
        /// <param name="pdwCount">A pointer to a DWORD value that receives the count of credentials.</param>
        /// <param name="pdwDefault">A pointer to a DWORD value that receives the index of the credential to be used as the default. If no default value has been set, this value should be set to CREDENTIAL_PROVIDER_NO_DEFAULT.</param>
        /// <param name="pbAutoLogonWithDefault">A pointer to a BOOL value indicating if the default credential identified by pdwDefault should be used for an auto logon attempt. An auto logon attempt means the Logon UI or Credential UI will immediately call GetSerialization on the provider's default tile.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetCredentialCount(
            out uint pdwCount,
            out uint pdwDefault,
            out int pbAutoLogonWithDefault
        )
        {
            Logger.Write();

            pdwCount = (uint)view.CredentialCount;

            pdwDefault = (uint)view.DefaultCredential;

            pbAutoLogonWithDefault = 0;

            return HRESULT.S_OK;
        }

        /// <summary>
        /// Gets a specific credential.
        /// The number of available credentials is retrieved by <see cref="GetCredentialCount"/>. 
        /// This method is used by the Logon UI or Credential UI in conjunction with GetCredentialCount to enumerate the credentials.
        /// </summary>
        /// <param name="dwIndex">The zero-based index of the credential within the set of credentials enumerated for this credential provider.</param>
        /// <param name="ppcpc">The address of a pointer to a ICredentialProviderCredential instance representing the credential.</param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int GetCredentialAt(uint dwIndex, out ICredentialProviderCredential ppcpc)
        {
            Logger.Write($"dwIndex: {dwIndex}");

            ppcpc = view.CreateCredential((int)dwIndex);

            return HRESULT.S_OK;
        }

        public virtual _CREDENTIAL_PROVIDER_USAGE_SCENARIO GetUsage()
        {
            return usage;
        }

        /// <summary>
        /// Called by the system during the initialization of a logon or credential UI to retrieve the set of users to show in that UI.
        /// Inherited from: <see cref="ICredentialProviderSetUserArray"/>
        /// </summary>
        /// <param name="users">
        ///     A pointer to an array object that contains a set of ICredentialProviderUser objects, 
        ///     each representing a user that will appear in the logon or credential UI. 
        ///     This array enables the credential provider to enumerate and query each of the user objects for their SID, 
        ///     their associated credential provider's ID, various forms of the user name, and their logon status string.
        /// </param>
        /// <returns><see cref="HRESULT"/></returns>
        public virtual int SetUserArray(ICredentialProviderUserArray users)
        {
            this.providerUsers = new List<ICredentialProviderUser>();

            users.GetCount(out uint count);
            users.GetAccountOptions(out CREDENTIAL_PROVIDER_ACCOUNT_OPTIONS options);

            Logger.Write($"count: {count}; options: {options}");

            for (uint i = 0; i < count; i++)
            {
                users.GetAt(i, out ICredentialProviderUser user);

                user.GetProviderID(out Guid providerId);
                user.GetSid(out string sid);

                this.providerUsers.Add(user);

                Logger.Write($"providerId: {providerId}; sid: {sid}");
            }

            return HRESULT.S_OK;
        }

        //Lookup the user by index and return the sid
        public virtual string GetUserSid(int dwIndex)
        {
            Logger.Write();

            //CredUI does not provide user sids, so return null
            if (this.providerUsers.Count < dwIndex + 1) return null;

            this.providerUsers[dwIndex].GetSid(out string sid);
            return sid;
        }
    }
}
