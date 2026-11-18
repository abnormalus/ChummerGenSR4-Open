# Private repository character storage

The desktop preview supports persistent GitHub pairing, a searchable filename
picker, local working copies, explicit uploads, and a link to file history on GitHub.
It uses the same default `characters/` directory and GitHub file revision as the PWA.

1. In **File → Connect Character Repository…**, create a private GitHub repository
   initialized with a README, or choose an existing private repository.
2. In GitHub, create a fine-grained personal access token scoped to that repository
   with **Contents: Read and write**. Enter the token, repository, and character folder
   in Chummer. The app remembers the connection in macOS Keychain or Windows
   Credential Manager. When GitHub supplies an expiration date, Chummer can prompt
   at expiration. If GitHub rejects the token first, the next repository action
   opens **Renew GitHub Token** with the existing repository and folder filled in.
   The GitHub link prefills the owner and **Contents: Read and write**. Select the
   same repository in GitHub, then paste the replacement into Chummer.
3. Use **Open from Repository…** to open a linked working copy. Use
   **Save to Repository…** to link an existing local character for the first time.
   Ordinary **Save** then syncs the current revision to GitHub without a success popup.
   **Save As** creates a local copy and switches the editor to that copy.
4. In **Options → Global**, enable or disable **Autosave characters** and set its
   interval in minutes. Autosave is enabled by default at five minutes. It saves
   pending edits to the current local file or linked GitHub destination and waits
   for you to choose a destination for an untitled character. Failed saves show an
   alert and keep pending edits available. Repeated autosave failures alert once
   per open character until a save succeeds.

The sections below describe the production requirements. Browser OAuth pairing,
in-app restore, and live desktop-to-companion round trips are still pending.
The existing desktop serializer is used for edited snapshots, so unknown XML
field preservation during an edit still needs verification.

## File menu and setup

- Add **Connect Character Repository…** to File. Open the browser at the appropriate
  GitHub authorization/setup page, with clear choices to create a private character
  repository or use an existing one. Prefill a new repository's name, description
  and private visibility. Confirm private visibility before enabling character writes.
- Allow selecting the repository and character folder after authorization, and
  disconnecting or changing the connection without removing local characters.
- Add **Open from Repository…** to File. Open a searchable window of `.chum` files
  from the selected repository. Search filenames and character name/alias where
  available; show loading, offline, empty, permission and expired-session states.
  Select/open a character through the normal desktop loader and unsaved-edit guard.
- Add **Save to Repository…** to File for the active character. For a new association,
  choose a remote filename; subsequent saves show the linked repository and file.
  Linked saves run quietly when successful and display errors on failure.
  Save As creates a local copy. Saves for unlinked characters stay local.

## Shared data and conflict contract

- Keep existing `.chum` XML as the exchange format. Preserve unknown fields, notes,
  nested equipment and portraits. Repository storage contains character files,
  separate from application source. Existing local originals remain intact.
- Save local working copies before network work; support editing while offline.
  An upload represents a captured revision. Its completion must not replace newer
  local edits or incorrectly mark them synchronized.
- Associate a remote file with its repository identity, path and last-known revision.
  Check that revision before saving; never force-overwrite another device's changes.
  Handle deleted/moved remote files and uncertain upload completion explicitly.
- If both copies changed, retain both and offer clear resolution. Do not automatically
  merge character XML, ammunition, damage, advancement or inventory state.
- Provide restore/history access with a retained local copy before replacing data.
  Use the same layout and revision rules in the PWA and native companions.

## Authorization and acceptance

- Release setup should use browser-based GitHub authorization scoped to the chosen
  repository. Keep secrets out of source and logs; desktop credentials belong in
  platform-protected storage. Connection failures must not discard an open character.
- Validate new/existing repository setup, searchable open, first/subsequent save,
  offline edits, revocation/expiry, simultaneous edits from two devices, failed or
  uncertain writes, recovery and preservation of unrelated XML on Mac and Windows.
- Verify desktop-to-PWA/mobile and back round trips with real production loaders.
  Private test data must remain outside the public source repository and CI artifacts.
