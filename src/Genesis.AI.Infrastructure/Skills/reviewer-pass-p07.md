# SKILL: reviewer-pass-p07
# Phase: P07 Information Governance — Phase 6

## Reviewer Pass — P07

**Purpose:** Systematic review of all IG sections before final output.

### Review Checklist

For each written IG section:
- [ ] Lawful basis is specified and matches UK GDPR Article 6/9
- [ ] Data classification is correct (no under-classification of special category data)
- [ ] Data minimisation — no unnecessary fields remain
- [ ] Retention periods match NHS RMCOP or have DPO exception documented
- [ ] All third-party transfers have DSA or parking lot item
- [ ] Encryption controls specified (at rest and in transit)
- [ ] Audit logging covers all personal data access/modification
- [ ] Privacy by design check passed

### Common Mistakes

- Recommending consent as the lawful basis for NHS clinical systems — use Public task (Art 6(1)(e))
- Setting retention to "6 months" for clinical records — must be 8 years minimum
- Missing DSA for third-party data processor (cloud provider, analytics tool)
- Not classifying NHS Number as special category data

### If a Section Fails Review

Create a 🟡 HIGH parking lot item. Update P07_REVIEW_LIST flag column. Re-write the section before marking as reviewed.
