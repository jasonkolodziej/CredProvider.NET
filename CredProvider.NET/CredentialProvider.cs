using CredProvider.NET.Interop2;
using System;
using System.Runtime.InteropServices;

namespace CredProvider.NET
{
    /// <summary>
    /// Implements the abstract outline for <see cref="CredentialProviderBase"/> 
    /// </summary>
    [ComVisible(true)]
    [Guid("00006d50-0000-0000-b090-00006b0b0000")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("CredProvider.NET")]
    public class CredentialProvider : CredentialProviderBase
    {
        public static CredentialView NotActive;        

        public CredentialProvider()
        {

        }

        protected override CredentialView Initialize(_CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus, uint dwFlags)
        {
            var flags = (CredentialFlag)dwFlags;

            Logger.Write($"cpus: {cpus}; dwFlags: {flags}");

            var isSupported = IsSupportedScenario(cpus);
            
            if (!isSupported)
            {
                if (NotActive == null) NotActive = new CredentialView(this) { Active = false };
                return NotActive;
            }

            var view = new CredentialView(this) { Active = true };
            /// States
            var userNameState = (cpus == _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_CREDUI) ?
                    _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_SELECTED_TILE : _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN;

            var confirmPasswordState = (cpus == _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_CHANGE_PASSWORD) ?
                    _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_BOTH : _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN;


            view.AddField(
                cpft: _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_TILE_IMAGE,
                pszLabel: "Icon",
                state: _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_BOTH,
                guidFieldType: Guid.Parse(CredentialView.CPFG_CREDENTIAL_PROVIDER_LOGO)
            );

            view.AddField(
                cpft: _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_EDIT_TEXT,
                pszLabel: "Username",
                state: userNameState
            );

            view.AddField(
                cpft: _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_PASSWORD_TEXT,
                pszLabel: "PIN",
                state: _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_SELECTED_TILE,
                guidFieldType: Guid.Parse(CredentialView.CPFG_SMARTCARD_PIN)
            );

            view.AddField(
                cpft: _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_PASSWORD_TEXT,
                pszLabel: "Confirm PIN",
                state: confirmPasswordState,
                guidFieldType: Guid.Parse(CredentialView.CPFG_SMARTCARD_PIN)
            );

            view.AddField(
                cpft: _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_LARGE_TEXT,
                pszLabel: "Dell CP",
                defaultValue: "Dell CP",
                state: _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_DESELECTED_TILE
            );

            return view;
        }

        private static bool IsSupportedScenario(_CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus)
        {
            switch (cpus)
            {
                /// Credential UI. This scenario enables you to use credentials serialized by the credential provider to be used as authentication on remote machines. 
                /// This is also the scenario used for over-the-shoulder prompting in User Access Control. 
                /// This scenario uses a different instance of the credential provider than the one used for CPUS_LOGON, 
                /// CPUS_UNLOCK_WORKSTATION, and CPUS_CHANGE_PASSWORD, so the state of the credential provider cannot be maintained across the different scenarios.
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_CREDUI:
                /// Workstation unlock. Credential providers that implement this scenario should be prepared to serialize credentials to the local authority for authentication. 
                /// These credential providers also need to enumerate the currently logged-in user as the default tile.
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_UNLOCK_WORKSTATION:
                    /// Workstation logon or unlock.
                    /// Credential providers that implement this scenario should be prepared to serialize credentials to the local authority for authentication.
                    /// <remarks>
                    ///     Starting in Windows 10, the CPUS_LOGON and CPUS_UNLOCK_WORKSTATION user scenarios have been combined. 
                    ///     This enables the system to support multiple users logging into a machine without creating and switching sessions unnecessarily. 
                    ///     Any user on the machine can log into it once it has been locked without needing to back out of a current session and create a new one. Because of this, CPUS_LOGON can be used both for logging onto a system or when a workstation is unlocked. However, CPUS_LOGON cannot be used in all cases. Because of policy restrictions imposed by various systems, sometimes it is necessary for the user scenario to be CPUS_UNLOCK_WORKSTATION. Your credential provider should be robust enough to create the appropriate credential structure based on the scenario given to it. Windows will request the appropriate user scenario based on the situation. Some of the factors that impact whether or not a CPUS_UNLOCK_WORKSTATION scenario must be used include the following. Note that this is just a subset of possibilities.
                    ///     The operating system of the device. Whether this is a console or remote session.
                    ///     Group policies such as hiding entry points for fast user switching, 
                    ///     or interactive logon that does not display the user's last name.
                    ///     Credential providers that need to enumerate the currently user logged into the system as the default tile 
                    ///     can keep track of the current user or leverage APIs such as WTSQuerySessionInformation to obtain that information.
                    /// </remarks>
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_LOGON:
                /// Password change. 
                /// This enables a credential provider to enumerate tiles in response to a user's request to change the password. 
                /// Do not implement this scenario if you do not require some secret information from the user such as a password or PIN. 
                /// These credential providers also need to enumerate the currently logged-in user as the default tile.
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_CHANGE_PASSWORD:
                    return true;
                /// Pre-Logon-Access Provider. Credential providers responding to this usage scenario must register under:
                ///     HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\PLAP Providers
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_PLAP:
                /// No usage scenario has been set for the credential provider. 
                /// The scenario is not passed to ICredentialProvider::SetUsageScenario. 
                /// If a credential provider stores its current usage scenario as a class member, 
                /// this provides an initialization value before the first call to <see cref="ICredentialProvider.SetUsageScenario"/>.
                /// <see cref="CredentialProviderBase"/>
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_INVALID:
                default:
                    return false;
            }
        }
    }
}
