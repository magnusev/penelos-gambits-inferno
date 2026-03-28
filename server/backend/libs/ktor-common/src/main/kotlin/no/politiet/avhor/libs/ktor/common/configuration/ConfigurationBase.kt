package no.politiet.avhor.libs.ktor.common.configuration

import com.typesafe.config.ConfigFactory
import io.ktor.server.config.HoconApplicationConfig
import no.politiet.avhor.libs.ktor.common.utils.EnvironmentType

abstract class ConfigurationBase {

    val config: HoconApplicationConfig =
        HoconApplicationConfig(
            ConfigFactory.parseResources(getResourceFromEnvironment()).resolve(),
        )

    val environment: EnvironmentType = EnvironmentType.valueOf(get("environment"))

    protected fun get(path: String): String =
        config.propertyOrNull(path)?.getString()
            ?: error("Missing required configuration property '$path'")

    protected fun getEnvVar(
        varName: String,
        defaultValue: String? = null,
    ): String = System.getenv(varName)
        ?: defaultValue
        ?: error("Missing required variable $varName")

    companion object {
        private fun getResourceFromEnvironment(): String {
            val env =
                System.getenv("APP_ENVIRONMENT")
                    ?.uppercase()
                    ?.let { EnvironmentType.valueOf(it) }
                    ?: EnvironmentType.LOCAL

            return when (env) {
                EnvironmentType.LOCAL -> "application-local.conf"
                EnvironmentType.DOCKER -> "application-docker.conf"
                EnvironmentType.OPPL -> "application-oppl.conf"
                EnvironmentType.PROD -> "application-prod.conf"
                EnvironmentType.TEST -> "application-test.conf"
            }
        }
    }

}
