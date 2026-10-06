---
title: Verify the Play Console account before linking a game service
date: 2026-10-06
category: workflow-issues
module: Google Play Games service activation
problem_type: workflow_issue
component: authentication
severity: high
applies_when:
  - "A supplied Play Console URL opens under a different cached Google identity."
  - "Multiple Google accounts have developer accounts with different eligibility states."
tags: [google-play, play-console, oauth, account-selection, unity]
---

# Verify the Play Console account before linking a game service

## Context

A supplied `/console/u/0/developers/...` policy URL redirected to developer-account selection. The current Google identity exposed a different developer account with identity/merchant verification failures; the second cached identity exposed another account already closed for inactivity. The intended account was not in either list. Creating credentials under the first available account would not satisfy the requested game connection.

## Guidance

Verify the developer-account identity from the live page and match the game's exact Android package before creating or modifying any Games project or OAuth client. The URL's `u/0` is not sufficient evidence that the intended Google account is selected. Use the visible account switcher and verify the resulting developer account, not guessed URL variations.

Keep developer-account IDs, Firebase project numbers and Play Games application IDs distinct. Do not enter an account/project number into Unity just because it passes a numeric input validator. Confirm a real Games application ID from the correct project's configuration/resources.

If the correct identity is unavailable, leave the local setup window ready and retain a Play Console sign-in tab for the user. Do not create a paid replacement account, alter identity/payment information, guess credentials or grant unrelated permissions to bypass an eligibility block.

## Why This Matters

OAuth credentials bind a package and signer to a particular game project. A wrong account/project can produce a successful-looking local configuration that still fails authentication and associates later work with the wrong administrative owner.

## When to Apply

Use for Games SDK activation, console publishing, signer registration and other work where cached Google identities can redirect a developer-console link.

## Examples

This task kept `PlayGamesSettings.mAppId` empty and the existing package/signing identity intact. The native console showed disabled app creation for the first account and an explicit inactivity closure for the second; the requested developer account was absent. The prerequisite is correct-account access or a verified Games application ID, not another source-code rewrite.

After the user signed in with the intended publisher identity, app creation succeeded under `mzkoreagames`. Cloud ownership was a separate constraint: the existing Firebase project belonged to the first identity, so the publisher's PGS Cloud selector was empty. Verify IAM roles before attributing an empty selector to an SDK failure. Prepare the exact account/project/role change for review and obtain action-time approval before broadening access; app-creation approval does not itself approve a Cloud Owner grant.

Related: [Project activation guide](../../google-play-account-setup.md). Official reference: [Play Games Services setup](https://developer.android.com/games/pgs/console/setup).
