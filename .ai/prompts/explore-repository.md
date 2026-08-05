# Prompt: Explore Repository

## Purpose
Answer "where does X live" and "how does X work" questions about GrandNode without guessing, and without reading the whole tree.

## Inputs Required
- Repository root.
- The question: a feature name, an entity, a URL, an admin screen, or a symptom.

## Steps

1. Read `.ai/knowledge/repository-map.md` first — it maps concerns to projects.
2. Narrow by question type:

   | Question | Start at |
   |---|---|
   | "Where is entity X stored?" | `src/Core/Grand.Domain/{Area}/` then grep for `IRepository<X>` |
   | "What happens when a customer does X?" | `.ai/knowledge/request-lifecycle.md`, then the controller in `src/Web/Grand.Web/Controllers/` |
   | "Where is this admin screen?" | `src/Web/Grand.Web.Admin/Areas/Admin/Controllers/` + matching `Views/` folder |
   | "Why is this value cached/stale?" | `.ai/knowledge/caching.md`, then grep the `CacheKey` constant |
   | "Who reacts when X is saved?" | `.ai/knowledge/domain-events.md`, then grep `EntityUpdated<X>` |
   | "Where is this string from?" | grep the resource key in `App_Data/Resources/` |
   | "Which permission guards this?" | `.ai/skills/permission-navigation.md`, then grep the `PermissionSystemName` |
   | "How do I extend this?" | `.ai/skills/project-structure.md` |

3. Follow the chain forward: controller → MediatR request → handler → business service → repository → domain entity.
4. Confirm each claim by opening the file. Do not report a path you have not read.
5. When the question is about a plugin or theme, check `src/Plugins/` before assuming the behavior lives in core.

## Mandatory Rules

1. Cite `path:line` for every claim.
2. Distinguish what the code does from what it appears to intend.
3. Say explicitly when a search found nothing rather than inferring the answer.
4. Do not modify files while answering an exploration question.

## Output Format

- **Answer**: two or three sentences, first.
- **Chain**: the call path, each step with `path:line`.
- **Related**: adjacent files worth reading next.
- **Uncertain**: anything not verified by reading the file.
