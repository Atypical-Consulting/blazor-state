# Phase 5: DevTools - Context

**Gathered:** 2026-01-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Built-in developer tools page at `/bustand-devtools` that provides real-time state inspection, action history tracking, time-travel debugging, and state diff visualization. This is a development-time debugging tool for developers, not end-user functionality.

</domain>

<decisions>
## Implementation Decisions

### Visual Presentation
- **Layout:** Sidebar + main panel (store list in left sidebar, selected store details in main area - like browser DevTools)
- **Sidebar content:** Store name + last update timestamp for each store
- **Color scheme:** Dark theme (developer-friendly, easier on eyes)
- **Real-time updates:** Auto-update without manual refresh (live subscription to state changes)
- **Availability:** Development only (DevTools route only available in DEBUG builds or Development environment)
- **Main panel organization:** Tabbed sections (tabs for Current State, History, Diff View)
- **Sidebar width:** Fixed width (not resizable)
- **Store navigation:** Search box in sidebar to filter store list by typing

### State Inspection
- **Display format:** Tree view with hierarchical expand/collapse for nested objects
- **Initial expansion:** First level expanded (top-level properties visible, nested objects collapsed)
- **Export options:** Copy to clipboard + download as .json file
- **Type visualization:** Both color coding and icons to distinguish data types (strings, numbers, booleans, null, objects, arrays)

### Action History
- **Timeline ordering:** Newest first (most recent actions at top, like log files)
- **History limit:** Last 100 actions kept in memory

### Time-Travel Behavior
- **Interaction:** Click on any history entry to jump to that state
- **Diff visualization:** Side-by-side view (old state on left, new state on right, like code diff)

### Claude's Discretion
- Action entry details (timestamp, action type, state summary format)
- Filtering/search within action history (decide if needed based on 100-action limit)
- Time-travel rewind mode (destructive vs preview vs temporary)
- Warning/safety measures for time-travel operations
- Exact styling details (spacing, typography, icon choices)
- Tree view implementation details (expand/collapse animation, indentation)
- Search box implementation (debouncing, highlight matches)

</decisions>

<specifics>
## Specific Ideas

- Sidebar layout inspired by browser DevTools (familiar to developers)
- Dark theme for reduced eye strain during debugging sessions
- Real-time updates to catch state changes as they happen without manual refresh
- Development-only availability to prevent accidental production exposure
- 100-action history provides good debugging window without excessive memory use

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 05-devtools*
*Context gathered: 2026-01-24*
