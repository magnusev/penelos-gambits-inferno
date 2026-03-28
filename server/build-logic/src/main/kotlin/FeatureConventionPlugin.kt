import com.penelosgambits.convention.addCompilerArguments
import com.penelosgambits.convention.addProjectDependency
import com.penelosgambits.convention.applyDetektConfiguration
import com.penelosgambits.convention.applyKtlintConfiguration
import com.penelosgambits.convention.applyTestConfiguration
import com.penelosgambits.convention.extentions.FeatureExtention
import com.penelosgambits.convention.extentions.collectSettings
import org.gradle.api.Plugin
import org.gradle.api.Project
import org.gradle.api.plugins.BasePluginExtension

class FeatureConventionPlugin : Plugin<Project> {
    override fun apply(target: Project) {
        with(target) {

            applyPlugins()
            applyKtlintConfiguration()
            applyDetektConfiguration()

            applyTestConfiguration()

            addCompilerArguments()

            addProjectDependency(":backend:libs:ulid")

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
