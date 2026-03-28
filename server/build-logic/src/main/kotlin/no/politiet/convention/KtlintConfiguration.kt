package no.politiet.convention

import org.gradle.api.Project
import org.gradle.kotlin.dsl.configure
import org.gradle.kotlin.dsl.withType
import org.jlleitschuh.gradle.ktlint.KtlintExtension
import org.jlleitschuh.gradle.ktlint.reporter.ReporterType
import org.jlleitschuh.gradle.ktlint.tasks.KtLintCheckTask
import org.jlleitschuh.gradle.ktlint.tasks.KtLintFormatTask

fun Project.applyKtlintConfiguration() {
    pluginManager.apply("org.jlleitschuh.gradle.ktlint")

    configure<KtlintExtension> {
        reporters {
            reporter(ReporterType.CHECKSTYLE)
            reporter(ReporterType.JSON)
        }

        filter {
            exclude("**/generate-resources/**")
                .setExcludes(setOf("*"))
        }
    }

    tasks.withType<KtLintCheckTask>().configureEach {
        notCompatibleWithConfigurationCache("ktlint plugin uses Project APIs not supported by CC")
    }
    tasks.withType<KtLintFormatTask>().configureEach {
        notCompatibleWithConfigurationCache("ktlint plugin uses Project APIs not supported by CC")
    }
}
