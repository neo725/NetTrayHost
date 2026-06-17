# Conventional Commits 規範

## 目的

所有 Git Commit Message 必須遵循 Conventional Commits 1.0.0 規範，以便：

* 提升 Commit 歷史可讀性
* 自動產生 Changelog
* 支援 Semantic Versioning (SemVer)
* 支援 Release Automation
* 讓 AI Agent 能夠一致地產生 Commit Message

---

# Commit Message 格式

```text
<type>[optional scope][!]: <description>

[optional body]

[optional footer(s)]
```

範例：

```text
feat(auth): add OAuth2 login support
```

```text
fix(api): resolve null reference exception when user not found
```

```text
feat!: remove deprecated v1 authentication endpoint
```

```text
feat(auth): add JWT token validation

BREAKING CHANGE: JWT secret format has changed.
```

---

# Type 類型定義

## feat

新增功能。

```text
feat(user): add profile image upload
```

適用：

* 新功能
* 新 API
* 新頁面
* 新服務
* 新模組

---

## fix

修復 Bug。

```text
fix(payment): correct tax calculation
```

適用：

* Bug 修復
* 邏輯錯誤修正
* 異常處理修正

---

## docs

文件變更。

```text
docs(readme): update installation guide
```

適用：

* README
* Wiki
* API 文件
* 註解文件

---

## style

不影響程式邏輯的格式調整。

```text
style(ui): format css indentation
```

適用：

* 排版
* 空白
* 換行
* 排序
* Prettier 格式化

不適用：

* 邏輯修改

---

## refactor

重構，不新增功能也不修復 Bug。

```text
refactor(service): simplify user validation flow
```

適用：

* 重構
* 模組拆分
* 提升可維護性

不適用：

* 新功能
* Bug 修復

---

## perf

效能優化。

```text
perf(query): optimize user lookup query
```

適用：

* SQL 優化
* Cache 優化
* 演算法優化

---

## test

測試相關。

```text
test(auth): add JWT validation tests
```

適用：

* Unit Test
* Integration Test
* E2E Test

---

## build

建置系統或相依套件變更。

```text
build(deps): upgrade Newtonsoft.Json to 13.0.3
```

適用：

* NuGet
* npm
* yarn
* Docker Build
* MSBuild

---

## ci

CI/CD 相關。

```text
ci(github): update release workflow
```

適用：

* GitHub Actions
* Azure Pipeline
* Jenkins
* GitLab CI

---

## chore

雜項工作。

```text
chore: update gitignore
```

適用：

* 不屬於其他類型
* 專案維護工作
* 設定調整

---

## revert

回滾 Commit。

```text
revert: feat(auth): add OAuth login
```

---

# Scope 規範

Scope 用來描述變更範圍。

格式：

```text
type(scope): description
```

範例：

```text
feat(auth): add OAuth login
fix(api): handle null user
refactor(service): simplify order workflow
docs(readme): update setup guide
```

常見 Scope：

```text
auth
api
ui
service
database
docker
config
deps
security
payment
user
admin
report
translation
```

若無明確範圍可省略：

```text
chore: update editorconfig
```

---

# Description 規範

使用英文。

採用：

```text
動詞原形 + 變更內容
```

推薦：

```text
add login endpoint
fix null reference issue
update deployment workflow
remove unused configuration
refactor validation service
```

避免：

```text
fixed bug
changes
update code
misc fixes
```

規則：

* 使用小寫開頭
* 不加句號
* 不超過 72 字元
* 描述本次 Commit 的主要目的

---

# Breaking Change

當 Commit 導致相容性破壞時：

方法一：

```text
feat!: remove legacy API
```

方法二：

```text
feat(api): redesign authentication flow

BREAKING CHANGE: authentication response format changed
```

可同時使用：

```text
feat(api)!: redesign authentication flow

BREAKING CHANGE: v1 login endpoint removed
```

---

# Semantic Version 對應

| Commit Type     | Version |
| --------------- | ------- |
| feat            | Minor   |
| fix             | Patch   |
| BREAKING CHANGE | Major   |

範例：

```text
1.2.3
```

新增功能：

```text
feat(api): add export endpoint
```

版本：

```text
1.3.0
```

修復問題：

```text
fix(api): handle empty result
```

版本：

```text
1.3.1
```

破壞性變更：

```text
feat!: remove deprecated endpoint
```

版本：

```text
2.0.0
```

---

# AI Agent Commit 產生規則

AI Agent 在產生 Commit Message 時必須：

1. 優先判斷變更目的：

```text
fix > feat > perf > refactor > docs > style > test > build > ci > chore
```

2. 一次 Commit 僅描述一個主要目的。

3. Description 必須說明：

```text
做了什麼
```

而不是：

```text
修改了哪些檔案
```

---

# Commit Message 範例

## 新增功能

```text
feat(user): add avatar upload support
```

## Bug 修復

```text
fix(api): prevent null reference when user not found
```

## 重構

```text
refactor(auth): extract token validation service
```

## 效能優化

```text
perf(database): reduce duplicate query execution
```

## 文件更新

```text
docs(readme): add docker deployment instructions
```

## Docker 相關

```text
build(docker): upgrade node image to v22
```

## GitHub Actions

```text
ci(github): add release workflow
```

## 相依套件更新

```text
build(deps): upgrade dapper to latest version
```

---

# 推薦規則（強制）

所有 Commit Message 必須：

✅ 使用 Conventional Commits

✅ 使用英文

✅ 使用小寫開頭

✅ 說明變更目的

✅ 使用適當 scope

❌ 不允許：

```text
update
fix bug
modify code
change file
misc
temp
wip
```

❌ 不允許中文 Commit Message

❌ 不允許無意義描述

---

# 最佳實務

推薦：

```text
feat(auth): add OAuth2 login support
fix(payment): prevent duplicate transaction creation
refactor(service): simplify order processing workflow
perf(query): optimize customer lookup query
```

避免：

```text
update code
fix stuff
change logic
misc updates
```

每個 Commit 應回答：

1. 做了什麼？
2. 為什麼做？
3. 是否造成 Breaking Change？

```
```
