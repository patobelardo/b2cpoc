# B2C Proof of Concept
B2C PoC using additional steps to get group memberships at the IdP

## Description

### Custom Policies

Here is where we extended the user journey with a new technical profile.

> This approach is useful in case you are not receiving information about group memberships at the claim, or you are receiving just Ids. In case you want to enable B2C to include group membership at claims, with no additional steps, please refer to **Custom Policies (include group membership at claim)**

Basically the changes are:

#### TrustFrameworkBase.xml

Added groupNames ClaimType
````xml
    <ClaimType Id="groupNames">
    <DisplayName>Collection with list of group names</DisplayName>
    <DataType>stringCollection</DataType>
    <UserInputType>Readonly</UserInputType>
    </ClaimType>
````
Added GetUserGroups TechnicalProfile
````xml
    <TechnicalProfile Id="GetUserGroups">
        <DisplayName>Retrieves security groups assigned to the user</DisplayName>
        <Protocol Name="Proprietary" Handler="Web.TPEngine.Providers.RestfulProvider, Web.TPEngine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" />
        <Metadata>
        <Item Key="ServiceUrl">https://<yourlocation>/api/membership/groupnames</Item>
        <Item Key="AuthenticationType">None</Item>
        <Item Key="SendClaimsIn">QueryString</Item>
        <Item Key="AllowInsecureAuthInProduction">true</Item>
        </Metadata>
        <InputClaims>
        <InputClaim Required="true" ClaimTypeReferenceId="issuerUserId" />
        </InputClaims>
        <OutputClaims>
        <OutputClaim ClaimTypeReferenceId="groupNames" />
        </OutputClaims>
        <UseTechnicalProfileForSessionManagement ReferenceId="SM-Noop" />
    </TechnicalProfile>
````

#### TrustFrameworkExtensions.xml

At one UserJourney, added an additional OrchestrationStep
````xml
         <OrchestrationStep Order="7" Type="ClaimsExchange">
          <ClaimsExchanges>
            <ClaimsExchange Id="GetUserGroups" TechnicalProfileReferenceId="GetUserGroups" />
          </ClaimsExchanges>
        </OrchestrationStep>
````

#### SignUpOrSigninCxDomain1.xml

Added the groupNames outputclaim
````xml
<OutputClaim ClaimTypeReferenceId="groupNames" />
````

### Custom Policies (include group membership at claim)

This is an **optional** step. 

In case you want to add the group membership you are receiving from your IdP as an output claim at B2C, you can do it changing the following files:

> In case of AAD, will need to change the app registration manifest to include group membership information, as explained [here](https://docs.microsoft.com/en-us/azure/active-directory/hybrid/how-to-connect-fed-group-claims#configure-the-azure-ad-application-registration-for-group-attributes)

#### TrustFrameworkBase.xml

Added 2 ClaimTypes
````xml
<ClaimType Id="groups">
  <DisplayName>Groups from AAD</DisplayName>
  <DataType>stringCollection</DataType>
</ClaimType>
 
<ClaimType Id="AADgroups">
  <DisplayName>Groups</DisplayName>
  <DataType>stringCollection</DataType>
</ClaimType>
````

#### TrustFrameworkExtension.xml

At the TechnicalProfile located at the ClaimsProvider that points to your IdP, addded the following OutputClaims
````xml
<OutputClaim ClaimTypeReferenceId="AADGroups" PartnerClaimType="groups"/>
````

#### SignUpOrSigninCxDomain1.xml

Added the AADGroups outputclaim
````xml
<OutputClaim ClaimTypeReferenceId="AADGroups" />
````

Now, your application that interacts with B2C will receive an AADGroups claim with groups information thet we god from the IdP.

### Membership information API

This is an Web API that uses Microsoft.Graph and will get group names based on a user membership (objectId).


