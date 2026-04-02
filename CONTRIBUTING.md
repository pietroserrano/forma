# Contributing to Forma

## Philosophy
Forma is an opinionated toolkit for .NET developers focused on:
- Functional programming patterns (Result, Option, etc.)
- Reusable architectural patterns
- Developer productivity utilities

This project does NOT aim to become a generic utility library.

---

## Contribution Types
- Bug Fixes → always welcome
- Improvements → evaluated case by case
- New Patterns → require proposal discussion
- New Modules → require architectural approval

---

## Proposal Flow
1. Open a "Proposal" issue
2. Describe:
   - Problem
   - Proposed solution
   - Alternatives
   - API impact
3. Wait for discussion
4. After approval → open PR

---

## Coding Guidelines
- Prefer immutability
- Avoid exceptions for flow control
- Prefer Result<TSuccess, TError>
- Option over null

---

## Testing
- Unit tests required
- Edge cases required

---

## Breaking Changes
Public API changes must be documented and discussed.
