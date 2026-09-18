# Optional rewarded video

After a failed round, the result screen offers an additional coin reward. The player can return to the workshop immediately or choose the video. There are no automatic ads at startup, on each death or during gameplay. The ordinary progression cohorts buy upgrades using gameplay coins without watching ads.

`Assets/ShooterSurvival/Resources/Ads/RewardedAdsSettings.asset` currently uses Google's official test units. The project includes Google Mobile Ads11.5.0 and External Dependency Manager1.2.187 as pinned local packages. Consent is requested before SDK initialization; the main-thread callback queue is created before the consent request.

The pinned Google Mobile Ads Android wrapper requires API24. The original and QA minimum are Android7.0/API24; Android6.0/API23 is no longer supported with this SDK. `RewardedAdsBuildValidator` rejects an incompatible floor before shader/IL2CPP work. The Firebase package guard pins the sameEDM1.2.187archive and its registry-verified hash rather than silently bypassing integrity checks.

The bonus equals the round's earned coins, bounded by the settings'20minimum and500maximum. Only the SDK's earned callback grants it. A round/attempt guard prevents duplicate payment and permits a late earned callback after the old result screen has been destroyed. Currency persistence remains local PlayerPrefs.

Native Editor verification:

- A90-coin offer changed the wallet from1165to1255.
- Trying the same offer again made no additional payment.
- Closing a fresh77-coin offer immediately kept the wallet at1255, ended the showing state and enabled Continue.
- These are the SDK's Editor placeholder controls, not network-served mobile advertisements.

The service has a45-second load timeout,60-second consent-update/SDK initialization timeout, stale-callback rejection, idle retry delays and cache expiry. An open consent/privacy form has no reading timeout. Actual device consent, offline/no-fill transport and production delivery still require a mobile test.

To activate real delivery, supply the app's AdMob Android/iOS app IDs in Google Mobile Ads settings and rewarded unit IDs in `RewardedAdsSettings`, set the applicable audience/consent configuration, and switch `useTestAds` off only after testing the build. No production IDs or publisher account have been invented. The old lobby picture promising20gems had no button implementation and is hidden; the functioning reward is the explicitly labeled coin offer on the result screen.
