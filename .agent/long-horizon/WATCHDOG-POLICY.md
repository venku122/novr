# Recovery policy

Single executor. On resume read SPEC, STATE and TASK-CONTRACT; compare Git branch/source, installed hashes and staged manifests. Never infer acceptance from old logs. Preserve stage and backups on failure. After three unchanged failures stop retries and change strategy. Physical baseline is the next required gate; do not implement UI/profile changes before it. No automatic restart/deploy/merge/push.
