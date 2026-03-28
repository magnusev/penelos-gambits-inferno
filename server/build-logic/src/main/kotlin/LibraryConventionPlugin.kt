import no.politiet.convention.addCompilerArguments
import no.politiet.convention.applyDetektConfiguration
import no.politiet.convention.applyKtlintConfiguration
import no.politiet.convention.applyTestConfiguration
import no.politiet.convention.extentions.FeatureExtention
import no.politiet.convention.extentions.collectSettings
import org.gradle.api.Plugin
import org.gradle.api.Project
import org.gradle.api.plugins.BasePluginExtension

class LibraryConventionPlugin : Plugin<Project> {
    override fun apply(target: Project) {
        with(target) {
            applyPlugins()
            applyKtlintConfiguration()
            applyDetektConfiguration()

            applyTestConfiguration()
            addCompilerArguments()

            val ext = extensions.create("feature", FeatureExtention::class.java)

            afterEvaluate {
                val settings = collectSettings(ext)

                setArchiveName(settings.name ?: project.name)
            }
        }
    }

    private fun Project.applyPlugins() {
        pluginManager.apply("org.jetbrains.kotlin.jvm")
        pluginManager.apply("com.autonomousapps.dependency-analysis")
    }

    private fun Project.setArchiveName(name: String) {
        val base = extensions.getByType(BasePluginExtension::class.java)
        base.archivesName.set(name)
    }

}
