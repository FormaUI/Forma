---
name: forma-legacy-coi
description: How to study the legacy Coi admin before designing a Forma component that replaces it — where the original markup, styles, code-behind, and jQuery widgets live, and how much fidelity to the original is intended. Use when designing any new component, porting a Coi admin screen or widget, or deciding whether to reproduce a legacy behaviour or improve on it.
---

# Studying the original before designing

Every component derives from the legacy Coi admin at `c:\Users\eyal\Development\Projects\2Site\Coi\Coi\`.

| What | Where |
|------|-------|
| Admin design system | `App_Themes/Cms/css-3.6.5.css`, `fa-3.6.5.css`, `global-3.6.5.css`, `responsive-3.6.5.css` |
| Page markup | `admin/*.aspx` and `admin/pops/*.aspx` |
| Behaviour and rendering helpers | Matching `*.aspx.cs` files |
| Replaced jQuery widgets | `admin/Client/{toggle,date,cp,modal,Tabs,jcrop,minibar}` |

- Do study the matching legacy markup, code-behind, styles, and widgets before designing a replacement.
- Do search that folder from the terminal, because it sits outside the workspace: `Select-String -Path 'c:\Users\eyal\Development\Projects\2Site\Coi\Coi\admin\*.aspx' -Pattern '...'`.
- Do preserve Coi's real states, vocabulary, edge cases, and required behaviour.
- Do reimplement visuals with Forma's design tokens and improve accessibility, affordances, and edge-case handling where useful.
- Do judge a component by whether it is good, not by whether it is pixel-identical to Coi.
- Don't treat Coi as a complete specification or an upper limit.
- Don't reproduce outdated visuals merely because Coi used them.
- Don't omit a worthwhile capability because Coi did not have it.

Coi's boxy gradient toggle becoming a token-driven iOS-style `FormaToggleSwitch` is the intended level of fidelity.
