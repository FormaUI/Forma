---
name: forma-documenting
description: The standard for prose — every document written for its reader, organized around the reader's questions, in the form each part needs, in manageable sentences and paragraphs, self-contained, free of implementation history it doesn't serve, and reviewed before it is finished; a summary or comment says what a thing is, a remarks block says how a non-trivial thing works, and a document says what its title promises: a specification the contract of a language, format or protocol, a design document the design, an implementation plan the steps and what landed, the component catalog the components that exist and the roadmap those still to build, the features status where the project stands and the changelog what each release changed, a README what its project is and how to use it, an instruction the rule. Use when writing, restructuring or reviewing any document — a README, guide, specification, design document, implementation plan, catalog or roadmap entry, the features status or the changelog — or a summary, comment, remarks block or stylesheet banner, and when deciding whether a code change should touch any prose at all.
---

# Documentation

Every piece of prose in a repository has one job, and the job decides what goes in it.

| Prose | Says | Never says |
|---|---|---|
| A summary or comment | What the thing is or does | How it does it, why it was chosen, what came before, what it does not do, what may or might happen, how it is to be used |
| A `<remarks>` block | How a non-trivial thing works | Why it was designed that way, what was tried |
| A specification | A language, format or protocol: its syntax, semantics, validity rules and observable guarantees, with examples that make the contract precise | Implementation architecture, delivery, resource management, usage walkthroughs |
| A design document | The design | The state of the code, what has been built, what changed since |
| An implementation plan | The steps, their gates, what landed and where it departed from the design, what is still open | The design itself, restated |
| The component catalog | Which components exist or are in progress, one line saying what each is, and the structure they live in | Parameters, options, approach, technical design, anything not yet started |
| The component roadmap | Which components are still to build, in what order, one line saying what each will be | Technical design, anything already built |
| The features status | Where the project stands: each feature with its state, the work that remains, the decisions open for the maintainer and what is deferred | The design, the steps of a plan under way, what a month added |
| The changelog | What each release changed for a consumer, newest first, and how to migrate from what it breaks | Work not yet landed, how a change was made, fixes that changed nothing a consumer relies on |
| A README | What its project is, what it is useful for, and how to use it | Who consumes it, how it works inside, what may or might happen |
| An instruction, a skill or the repository's working rules | How to work | Project structure, project names, the change that prompted the rule |

## Write for readers

Make documentation easy to understand, scan and return to. Preserve technical precision and explain the subject fully enough for its intended reader.

Treat readability as part of correctness. A technically accurate explanation still needs revision when its structure makes the answer difficult to find or understand.

### Organize around the reader's questions

- Identify the intended reader and the questions the document must answer.
- Arrange sections around those questions, in the order the reader needs them.
- Give sections descriptive headings that make specific answers easy to find.
- Start each section with its answer or governing rule. Follow with the explanation, examples and relevant exceptions.
- Introduce concepts before relying on them. Define unfamiliar terms where readers first need them.
- Keep the normal procedure together. Give advanced configuration and uncommon cases clearly named sections.
- Keep prerequisites, limitations and warnings beside the action or rule they affect. Do not hide a condition needed for successful use in a later section.
- Add a short navigation list when a long document benefits from one.

Do not organize ordinary documentation around the order in which the author discovered, discussed or implemented its subject.

### Choose the appropriate form

- Use paragraphs to explain an idea, establish context or develop an argument.
- Use bullets for parallel rules, alternatives or independent facts.
- Use numbered lists for procedures whose order matters.
- Use tables for mappings or comparisons with consistent attributes.
- Use code blocks for code, commands, configuration and syntax.
- Use diagrams when relationships, dependencies or event order are easier to understand visually.
- Use examples when a rule becomes clearer through a concrete input, action or result.

Choose the form that communicates the information most clearly. Design documents may use paragraphs, lists, tables, examples and diagrams.

Do not turn a dense paragraph into an equally dense bullet or table cell. When an item needs several explanations, give it a subsection. Keep table cells concise and place substantial explanations beside the table.

Do not make every sentence a bullet or introduce a heading for every sentence. Let related sentences form a coherent paragraph.

### Write manageable sentences and paragraphs

- Give each paragraph one main point.
- State the subject and action directly.
- Use concrete names and familiar words. Keep established technical terms where they provide precision.
- Name the relevant component or service again when a pronoun would be ambiguous. Repeating a name is preferable to making the reader guess.
- Separate a rule from its rationale, example and exception when combining them would require the reader to untangle several clauses.
- Replace long inline enumerations with a list or table.
- Review sentences longer than about 35 words and paragraphs longer than about 100 words. Treat these as editing signals, not mechanical limits.
- Preserve natural flow. Do not produce disconnected, one-sentence paragraphs merely to satisfy a length target.
- Use emphasis selectively to help readers find important terms or conditions.

Shorter is useful when it makes the explanation clearer. Do not compress several ideas into one sentence or remove necessary detail merely to reduce the word count.

### Make the document sufficiently self-contained

- Explain what the reader needs to understand the subject or complete the task in this document.
- Repeat information from elsewhere when it makes the explanation clearer, more complete or easier to follow.
- Use links for additional depth and related subjects. Do not substitute a link for an essential explanation or required step.
- Keep repeated facts consistent when changing them.
- Keep an overview useful in its own right, even when detailed documents cover the same material.

Do not remove useful information solely because another document contains it. Duplication is acceptable; contradictions and stale statements are defects.

### Match the document's purpose

- README: explain what the project is, why it is useful, show a representative example and provide a clear, complete path to first use. Make requirements and availability easy to find. Provide navigation to further guidance.
- How-to guide: state the goal and prerequisites, give ordered steps, show how to recognize success and explain relevant failure cases.
- Repository guide: provide a navigable map of locations, responsibilities, dependencies and common commands.
- Design document: explain concepts, relationships, constraints and the rationale needed to understand and preserve the design.
- Specification: state precise syntax, semantics, validity rules, defaults, exceptions and observable guarantees. Distinguish examples from normative requirements.
- Implementation plan: state the work sequence, dependencies, completion criteria, progress and unresolved decisions.
- Status document: make current capabilities, limitations, remaining work and open decisions easy to locate.
- Changelog or migration guide: explain the changes relevant to the reader and any actions required to adopt them.
- Historical account or decision record: explain the relevant chronology, alternatives and reasons, since those are part of its subject.

Adapt the structure to the document's actual purpose. Do not impose identical sections on every document.

### Keep implementation history in its proper context

- Describe current behavior, the contract or the intended design, according to the document's purpose.
- Do not narrate which patch introduced something, what was tried first, what an earlier implementation did or which development step produced it unless that history serves the document's subject.
- Include rationale when it explains a constraint, a tradeoff or a decision the reader needs to make.
- State that rationale directly. Explain why a rule exists without recounting the development session that led to it.
- Keep progress and completion tracking in documents intended to track work.
- Include historical comparisons where they are useful to the task, such as explaining a breaking change in a migration guide.

Do not turn a README, repository guide, specification or design document into a chronological account of implementation.

### Edit existing documents without losing meaning

- Preserve requirements, qualifications, defaults, exceptions and guarantees when restructuring accurate material.
- Do not weaken "must" into "should", broaden a guarantee or remove a condition merely to shorten a sentence.
- Preserve identifiers, commands, examples, anchors and tooling markers unless changing them is part of the task.
- Correct outdated statements when the task changes the behavior they describe. Do not preserve a known error for the sake of preserving the original text.
- Reorganize the affected section when necessary. Do not keep appending clauses to an already crowded paragraph.
- Keep necessary context beside the explanation that relies on it, even when that context also appears elsewhere.
- Remove irrelevant implementation history from ordinary explanatory sections. Retain any rationale or current constraint that the history was conveying.
- Read the surrounding sections after an edit so the document remains coherent.

### Review before finishing

Read the result as a person arriving without the conversation.

- Can the reader identify the document's purpose immediately?
- Can the reader find specific answers by scanning its headings?
- Does each paragraph or list item have one clear purpose?
- Are any sentences carrying several rules, examples and exceptions at once?
- Would a list, table, example or diagram make a dense passage clearer?
- Can the intended reader follow the main procedure with the information given?
- Are rules, defaults, examples and exceptions easy to distinguish?
- Does the document explain its subject without unnecessary implementation history?
- Does useful repetition remain consistent?
- Did restructuring preserve the substantive meaning and qualifications?
- Do links, anchors, code fences, tables and examples still work?

Inspect the rendered Markdown when a preview is available. Source-line wrapping alone does not improve the rendered document.

Fix readability and correctness failures before presenting the result.

## Summaries and comments

- Do state what a thing is or does in an XML `<summary>`, a stylesheet banner, an inline comment or a README sentence, in US English throughout, identifiers and paths included; a platform's own name keeps the platform's spelling, as `aria-labelledby` does.
- Do give every public type and public member a one-line `<summary>`, and put `/// <inheritdoc />` on every override and interface implementation rather than restating what the base member documents.
- Do write an inline comment only where the code cannot express an important constraint — a necessary `!important`, a non-obvious vendor-prefixed property, an off-by-one invariant — and keep it to one line.
- Do describe a member in its own name: a parameter named `Items` is described as items, never renamed to the concept it stands for.
- Do describe a member at the full breadth of its type: a parameter typed as a list of toolbar items is "the items the toolbar renders", never "the action buttons", when the item type also covers groups, menus, toggles and readouts.
- Do say what a thing can do where the code allows either, never the general case for the optional one: a surface whose items may or may not be resources "can list" a resource, it does not "list" resources.
- Do name a component by its component name wherever the surrounding text has not already established which component is meant; a bare noun such as "the explorer" belongs only inside that component's own document, where a reader arriving at the sentence alone can still tell which component it is.
- Do re-read the prose around whatever you change and fix what the change made false: a class summary naming what a component is built on, a "no JavaScript" banner on a component that gained interop, a sentence saying another component *should* adopt something it now has.
- Don't write how a thing does its work in a summary or comment; a reader who needs the mechanism reads the code, and a mechanism the code cannot show goes in `<remarks>`.
- Don't write history, rejected alternatives, decision arguments, roadmap position, call sites, mirrors or other cross-references; a comment that ties a type to its first consumer narrows it falsely.
- Don't write what a thing might do, does not do, or would otherwise have done; a sentence about code that is not there sends the reader looking for it.
- Don't add a clause about conditions, edge cases, defaults or usage: no "or null when…", no "fewer than asked for where…", no "Supply this or X, not both". Null is described only where it is a value with a meaning of its own, as "null matches any" is.
- Don't mark or track progress in a comment — no "step 3", "for now", "later", "temporary", "TODO" — and don't narrate the situation the code runs in to set a scene for the reader.
- Don't restate the code or pad a file with prose; delete a comment that carries nothing rather than rewording it.

## READMEs

Every project has a README, a package and an internal assembly alike, and it answers what its reader opened it to find.

- Do say what the project is, what it is useful for, and how to use it: for a package, how a host installs, registers and uses it; for an internal assembly, how the library's own code uses it.
- Do order it by what the reader needs first, and cut every sentence that does not serve that reader.
- Don't list who consumes the project, narrate what may or may not happen, or explain internals the reader did not come for.

## Remarks

- Do put the mechanism of a non-trivial member in a `<remarks>` block under its summary when a reader could not recover it from the code: the traversal order that decides which of two nested elements matches first, a selection frozen because a closing dialog returns a collapsed caret, a task held rather than its result so racing callers share one load.
- Do describe the mechanism, not the reasoning that chose it, and keep the summary above it to what the member is.
- Don't add remarks to say something is simple, or to repeat the summary in more words.

## Design documents

A design document states a design: the concepts, their names, how they relate, and the rules a change to that area must keep. It is read before changing the area, which is why the repository's working rules point at it. While the area is being built, the implementation plan stands beside it, a second document holding everything about carrying the design out: the steps and their gates, what landed at each, where it departed from the design, and what is still open. A design outlives its plan, so a finished area carries the design alone.

- Do write the design as it is meant to be, in the form that states each part most clearly, with a code sketch only where the shape itself is the design.
- Do define a term of art where it is introduced, and in the repository's shared vocabulary when the whole codebase will speak it.
- Do change a design document when the design changes, and leave it alone when code lands that follows it.
- Do record progress in the implementation plan: a step that landed, a departure from the design and why, a survey the plan rested on, what remains.
- Do start both documents together when an area is designed, and split a document that mixes the two along that line.
- Do retire a plan once every step has landed and nothing is open, moving any reasoning it holds alone into the design first, since what a finished plan records is the code and the history; retire one whose remaining steps nobody is building the same way, moving those steps to the features status as remaining work.
- Don't record the state of the code in a design document: no status paragraph, no "built" markers, no list of what departed from a sketch and why, no commit references.
- Don't carry technical detail that belongs in the code and its summaries — signatures, member lists, file names, test names, counts.
- Don't restate the design in the implementation plan beyond what a step needs to be understood; the plan refers to the design's sections for the rest and says what was done about them.

## Specifications

A specification states what an implementation must guarantee, and the design beside it states how the implementation achieves that guarantee.

- Do define a language's, format's or protocol's syntax, semantics, validity rules and observable guarantees, evaluation order, null propagation, observation semantics and error behavior among them, with examples wherever they make the contract precise.
- Do give a specification its own document, `{area}-specification.md`, beside the design of the area that implements it, each linking to the other.
- Don't put implementation architecture, delivery order, resource management or usage walkthroughs in a specification; they belong to the design and the implementation plan.

## The component catalog and roadmap

The catalog lists the components that exist and the roadmap the components still to build, both alphabetically, unnumbered, each opening with a structure of folders and components and a count of what it lists: the catalog's structure holds the implemented components and those in progress, marked apart, and the roadmap's holds the ones not yet started.

- Do add a component to the roadmap when it is planned, move it into the catalog's structure when its implementation starts, and move its entry to the catalog when it is approved.
- Do keep an entry to its name, one line of what it is, and the rows the documents keep on purpose, such as a component's origin and where it is used.
- Do refer to a component by name alone, never by a number or a position.
- Don't put parameters, options, the rendering approach, notes on mechanics or any other API or design detail in an entry; a component that needs a technical design gets its own design document.

## The features status and the changelog

The features status says where the project stands and where it goes next, and the changelog what each release changed. Each points at the documents that state a feature for their depth.

- Do change the features status in the change that changes what it records: a feature landing or changing state, a plan's step landing, a decision opened or taken, work deferred or taken up.
- Do give every open decision a proposal, and move a decision out once the maintainer takes it, into the document the decision changes.
- Do add to the changelog's newest entry, in the commit that makes it, each change a consumer can notice, naming each feature by the document that states it, and say how to migrate from a change that breaks what the release before it allowed; the first such change after a release opens the next entry and advances the declared version, in the same commit.
- Don't record in the changelog a fix, a refactoring or a document that changes nothing a consumer relies on.
- Don't add to the record a project kept before its first release; it stays as that period's archive, which the changelog links to.

## Instructions

An instruction — a skill, or the repository's working rules — describes how to work, so it stays true when the project around it changes.

- Do state a practice or a decision rule, with an example only where the rule is unclear without one: generic but meaningful to the reader, drawn from the repository's own code or from well-known real-world code, and phrased so it stays true when names change. A record of a past rename or incident is not an example.
- Do refer to a document by its role, "the repository's component documentation", never by its file name or path, and never by a path that assumes how repositories are checked out side by side.
- Don't put project structure, component names, directory layouts or another project's name in an instruction; those are facts about a project and belong in its documentation.
- Don't tell the reader to consult another project; state the principle that project would have taught, directly.

## When a code change touches prose

- Do fix a summary, comment, banner or README sentence the change made false, as part of the change.
- Do update an instruction when the change changes a rule, a design document when it changes the design, the implementation plan when a step lands or departs from the design, and the features status and the changelog when a feature lands or changes.
- Don't update an instruction, a design document, a catalog entry or a roadmap entry because code landed; landing a step is not a rule change and not a design change.
