import com.penelosgambits.convention.extentions.OpenApiGeneratorExtension
import com.penelosgambits.convention.extentions.collectSettings
import org.gradle.api.Plugin
import org.gradle.api.Project
import org.gradle.kotlin.dsl.configure
import org.openapitools.generator.gradle.plugin.extensions.OpenApiGeneratorGenerateExtension

class OpenApiGeneratorConventionPlugin : Plugin<Project> {

    override fun apply(target: Project) = with(target) {
        applyPlugins()
        addGeneratedSourceDir()

        val ext = extensions.create("openApiConvention", OpenApiGeneratorExtension::class.java)

        afterEvaluate {
            val settings = collectSettings(ext)
            configureOpenApiGenerate(settings.inputSpec, settings.modelPackage)
        }

        configureTaskDependencies()
    }

    private fun Project.applyPlugins() {
        pluginManager.apply("org.openapi.generator")
    }

    private fun Project.addGeneratedSourceDir() {
        extensions.configure<org.gradle.api.tasks.SourceSetContainer> {
            named("main") {
                java.srcDir("build/generate-resources/main/src/main/kotlin")
            }
        }
    }

    private fun Project.configureOpenApiGenerate(inputSpec: String, modelPackage: String) {
        extensions.configure<OpenApiGeneratorGenerateExtension> {
            this.inputSpec.set(inputSpec)
            this.modelPackage.set(modelPackage)
            generatorName.set("kotlin")
            library.set("jvm-ktor")
            additionalProperties.set(
                mapOf(
                    "apis" to "false",
                    "supportingFiles" to "false",
                    "serializationLibrary" to "kotlinx_serialization",
                ),
            )
        }
    }

    private fun Project.configureTaskDependencies() {
        tasks.named("runKtlintCheckOverMainSourceSet") {
            dependsOn(tasks.named("openApiGenerate"))
        }

        tasks.named("runKtlintFormatOverMainSourceSet") {
            dependsOn(tasks.named("openApiGenerate"))
        }

        tasks.named("compileKotlin") {
            dependsOn("openApiGenerate")
        }
    }
}

