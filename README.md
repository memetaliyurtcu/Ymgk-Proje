# Ymgk-Proje (ARFishing)

> AR-based marine ecosystem education app for ages 7–12.
> Working title: **"Görünenin Ötesinde Bir Deniz"**

Unity 6 · AR Foundation 6.4 · URP · Input System · XRI 3.4

---

## What is this?

A classroom AR experience: 5 children + 1 tablet + 10 marker cards. Each card shows a sea creature (octopus, coral, jellyfish, etc.) when scanned — 3D model appears on the card, Turkish audio narration plays, info panel shows habitat / diet / threats. Teacher runs a mini quiz, summary shows ecosystem map. **35–45 minute activity**.

Despite the repo name, this is **not** a fishing game.

## Documentation

**Start here** → [`Docs/index.html`](Docs/index.html) — landing page with role-based navigation ("Sen kimsin?" chips routing to the right doc).

Seven production documents (each MD + HTML):

| Doc | Audience | What it covers |
|---|---|---|
| [Artist Brief](Docs/ArtistBrief.html) | Vendors (3D, voice, illustrator, print, SFX) | Full production spec + per-creature direction + TR narration scripts |
| [Teacher Guide](Docs/TeacherGuide.html) | Educators | Session script, observation rubric, troubleshooting, hızlı referans kartı |
| [Engineer Onboarding](Docs/EngineerOnboarding.html) | New Unity devs | Repo tour, 8-faz history, module map, FSM, scene wiring, dev tasks |
| [Build & Distribution](Docs/BuildAndDistribution.html) | Shipping devs | Android (ARCore) + iOS (ARKit) build pipeline, signing, store dağıtım |
| [Device Test Protocol](Docs/DeviceTestProtocol.html) | QA | Real-device QA: smoke + functional + performance + privacy verification + sign-off |
| [Privacy Policy](Docs/PrivacyPolicy.html) | Store listing | "Zero data collected" + KVKK/GDPR/COPPA + store form answers |
| [Pilot Feedback Protocol](Docs/PilotFeedbackProtocol.html) | Post-pilot iteration | Feedback aggregation + decision matrix for doc/code/content updates |

**Engineering rulebook** → [`CLAUDE.md`](CLAUDE.md). asmdef structure, scene wiring procedure, naming conventions, MVP creature set, privacy stance, AR Foundation 6.x API rules. Keep open during every dev session.

## Quick start (Unity dev)

```
1. Install Unity Hub + Unity 6000.4.9f1 (with Android + iOS Build Support)
2. Open this folder in Unity Hub
3. Open Assets/ARFishing/Scenes/Bootstrap.unity
4. Press Play
```

If you see scene wiring issues on first run, work through `Docs/EngineerOnboarding.html` §7 then `CLAUDE.md` "Scene wiring".

For the first test session: `ARFishing → Create MVP Content (20 creatures + 8 tasks)` + `Create Placeholder Models/Audio/Icons/SFX/UI Sprites`.

## Privacy

**Zero data collected.** No analytics, no network calls, no third-party SDKs. Internet permission removed from Android manifest. KVKK / GDPR / COPPA compliant by being collect-nothing.

Details → [`Docs/PrivacyPolicy.html`](Docs/PrivacyPolicy.html).

## Status

MVP code-complete through faz F10 (accessibility + i18n foundation). All 20 creatures scaffolded with placeholder assets. Awaiting real artist deliverables (3D, voiceover, cards, icons, SFX, illustrations) per Artist Brief, then pilot test program per Teacher Guide + Device Test Protocol + Pilot Feedback Protocol iteration cycle.

## License

(Set before distribution — likely proprietary for the education product.)
